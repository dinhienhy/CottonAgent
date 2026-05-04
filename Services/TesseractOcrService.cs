using CBAS.Web.DTOs;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace CBAS.Web.Services;

public class TesseractOcrService : IOcrService
{
    private readonly bool _tesseractAvailable;
    private readonly ILogger<TesseractOcrService> _logger;

    public bool IsAvailable => _tesseractAvailable;

    public TesseractOcrService(ILogger<TesseractOcrService> logger)
    {
        _logger = logger;
        _tesseractAvailable = CheckTesseractCli();

        if (_tesseractAvailable)
            _logger.LogInformation("Tesseract OCR ready (CLI mode)");
        else
            _logger.LogWarning("Tesseract OCR not available - 'tesseract' command not found. Manual input only.");
    }

    public async Task<OcrResult> ProcessHVIPdfAsync(Stream pdfStream, string fileName)
    {
        if (!IsAvailable)
        {
            return new OcrResult
            {
                Success = false,
                ErrorMessage = "OCR không khả dụng (tesseract chưa cài đặt)"
            };
        }

        try
        {
            // Step 1: Extract images from PDF using PdfPig
            var imageFiles = await ExtractImagesToTempFiles(pdfStream);

            if (imageFiles.Count == 0)
            {
                return new OcrResult
                {
                    Success = false,
                    ErrorMessage = "Không tìm thấy hình ảnh trong PDF"
                };
            }

            // Step 2: Run tesseract CLI on each image
            var allText = new StringBuilder();
            int pageCount = 0;

            foreach (var imgFile in imageFiles)
            {
                try
                {
                    var text = await RunTesseractCli(imgFile);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        allText.AppendLine(text);
                        pageCount++;
                        _logger.LogInformation("OCR page {Page}: {Chars} chars from {File}",
                            pageCount, text.Length, Path.GetFileName(imgFile));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("OCR failed on image {File}: {Error}", imgFile, ex.Message);
                }
                finally
                {
                    try { File.Delete(imgFile); } catch { }
                }
            }

            if (pageCount == 0)
            {
                return new OcrResult
                {
                    Success = false,
                    ErrorMessage = "OCR không đọc được ảnh nào"
                };
            }

            var rawText = allText.ToString();

            // Step 3: Parse HVI fields from OCR text
            var result = ParseHVIFromOcrText(rawText);
            result.Success = true;
            result.Confidence = 0.5f; // CLI doesn't provide confidence; default 50%
            result.RawText = rawText;

            _logger.LogInformation("OCR done for {File}: pages={Pages}, mic={Mic}, len={Len}, str={Str}",
                fileName, pageCount, result.Micronaire, result.Length, result.StrengthGPT);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR error for {File}", fileName);
            return new OcrResult
            {
                Success = false,
                ErrorMessage = $"Lỗi OCR: {ex.Message}"
            };
        }
    }

    private async Task<string> RunTesseractCli(string imageFile)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "tesseract",
            Arguments = $"\"{imageFile}\" stdout -l eng --psm 3",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            _logger.LogWarning("tesseract exit code {Code}: {Err}", process.ExitCode, error);
        }

        return output;
    }

    private async Task<List<string>> ExtractImagesToTempFiles(Stream pdfStream)
    {
        var files = new List<string>();
        try
        {
            pdfStream.Position = 0;
            var pdfBytes = new byte[pdfStream.Length];
            await pdfStream.ReadAsync(pdfBytes, 0, pdfBytes.Length);

            using var document = PdfDocument.Open(pdfBytes);
            int idx = 0;
            foreach (var page in document.GetPages())
            {
                foreach (var image in page.GetImages())
                {
                    try
                    {
                        byte[]? imgBytes = null;
                        string ext = "png";

                        // TryGetPng decodes the PDF-internal format to standard PNG
                        if (image.TryGetPng(out var pngBytes) && pngBytes.Length > 1000)
                        {
                            imgBytes = pngBytes;
                        }
                        else
                        {
                            // Fallback: raw bytes (may be JPEG)
                            var raw = image.RawBytes.ToArray();
                            if (raw.Length > 1000)
                            {
                                imgBytes = raw;
                                // Detect JPEG header
                                if (raw.Length > 2 && raw[0] == 0xFF && raw[1] == 0xD8)
                                    ext = "jpg";
                            }
                        }

                        if (imgBytes != null)
                        {
                            var tempFile = Path.Combine(Path.GetTempPath(), $"ocr_{idx++}.{ext}");
                            await File.WriteAllBytesAsync(tempFile, imgBytes);
                            files.Add(tempFile);
                            _logger.LogInformation("Saved temp image: {File} ({Size} bytes, {W}x{H})",
                                tempFile, imgBytes.Length, image.WidthInSamples, image.HeightInSamples);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to extract image: {Error}", ex.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract images from PDF");
        }

        return files;
    }

    private bool CheckTesseractCli()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.Start();
            var output = process.StandardError.ReadToEnd() + process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            _logger.LogInformation("Tesseract CLI found: {Version}", output.Split('\n').FirstOrDefault()?.Trim());
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private OcrResult ParseHVIFromOcrText(string text)
    {
        var result = new OcrResult();

        // Normalize text: fix common OCR mistakes
        text = text.Replace("'", "'").Replace(""", "\"").Replace(""", "\"");

        // --- Micronaire ---
        // Patterns: "Mic 4.47", "MIKE 4.47", "Micronaire 4.47", "MIC: 4.47"
        var micMatch = Regex.Match(text, @"(?:Mic(?:ronaire)?|MIKE?)\s*[:\.]?\s*(\d+\.?\d*)", RegexOptions.IgnoreCase);
        if (micMatch.Success && decimal.TryParse(micMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var mic))
        {
            if (mic >= 2.0m && mic <= 7.0m) // Valid micronaire range
                result.Micronaire = mic;
        }

        // --- Length (UHML) ---
        // Patterns: "Len 1.13", "UHML 28.70", "Length 112.87", "SL 28.5"
        var lenMatch = Regex.Match(text, @"(?:UHML|Len(?:gth)?|Staple|SL)\s*[:\.]?\s*(\d+\.?\d*)", RegexOptions.IgnoreCase);
        if (lenMatch.Success && decimal.TryParse(lenMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var len))
        {
            result.Length = len;
        }

        // --- Strength (GPT) ---
        // Patterns: "Str 30.84", "GPT 30.84", "Strength 30.84"
        var strMatch = Regex.Match(text, @"(?:Str(?:ength)?|GPT|Tenacity)\s*[:\.]?\s*(\d+\.?\d*)", RegexOptions.IgnoreCase);
        if (strMatch.Success && decimal.TryParse(strMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var str))
        {
            result.StrengthGPT = str;
        }

        // --- Uniformity ---
        // Patterns: "Unif 81.33", "UI 81.33", "Uniformity 81.33"
        var unifMatch = Regex.Match(text, @"(?:Unif(?:ormity)?|UI)\s*[:\.]?\s*(\d+\.?\d*)", RegexOptions.IgnoreCase);
        if (unifMatch.Success && decimal.TryParse(unifMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var unif))
        {
            if (unif >= 50m && unif <= 100m) // Valid uniformity range
                result.Uniformity = unif;
        }

        // --- Color Rd ---
        // Patterns: "Rd 74.67", "Rd: 74.67"
        var rdMatch = Regex.Match(text, @"Rd\s*[:\.]?\s*(\d+\.?\d*)", RegexOptions.IgnoreCase);
        if (rdMatch.Success && decimal.TryParse(rdMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rd))
        {
            result.ColorRd = rd;
        }

        // --- +b ---
        var bMatch = Regex.Match(text, @"\+b\s*[:\.]?\s*(\d+\.?\d*)", RegexOptions.IgnoreCase);
        if (bMatch.Success && decimal.TryParse(bMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var pb))
        {
            result.PlusB = pb;
        }

        // --- Color Grade ---
        // Patterns: "Color Grade 41-1", "CG 41-1", "Classing 21-1"
        var cgMatch = Regex.Match(text, @"(?:Color\s*Grade|CG|Classing|Grade)\s*[:\.]?\s*(\d{2}-\d)", RegexOptions.IgnoreCase);
        if (cgMatch.Success)
        {
            result.ColorGrade = cgMatch.Groups[1].Value;
        }

        // --- Leaf ---
        // Patterns: "Leaf 3.32", "Lf 3.32", "Trash 3"
        var leafMatch = Regex.Match(text, @"(?:Leaf|Lf|Trash)\s*[:\.]?\s*(\d+\.?\d*)", RegexOptions.IgnoreCase);
        if (leafMatch.Success && decimal.TryParse(leafMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var leaf))
        {
            result.Leaf = leaf;
        }

        // --- Total Bales ---
        var balesMatch = Regex.Match(text, @"(?:Total\s*Bales?|Bales?)\s*[:\.]?\s*(\d+)", RegexOptions.IgnoreCase);
        if (balesMatch.Success && int.TryParse(balesMatch.Groups[1].Value, out var bales))
        {
            result.TotalBales = bales;
        }

        // --- Crop Year ---
        var yearMatch = Regex.Match(text, @"(?:Crop\s*Year|Season|CY)\s*[:\.]?\s*(20\d{2})", RegexOptions.IgnoreCase);
        if (yearMatch.Success)
        {
            result.CropYear = yearMatch.Groups[1].Value;
        }

        // Try to extract averages from tabular data (common HVI format)
        // Look for patterns like: AVG or AVERAGE followed by numbers
        TryParseAverageLine(text, result);

        return result;
    }

    private void TryParseAverageLine(string text, OcrResult result)
    {
        // Many HVI reports have an "Average" or "AVG" line with all values
        // Pattern: AVG  4.47  112.87  81.33  30.84  74.67  8.23  3.32
        var avgMatch = Regex.Match(text, @"(?:AVG|Average|AVERAGE|Mean|MEAN)[^\n]*\n?([^\n]+)", RegexOptions.IgnoreCase);
        if (!avgMatch.Success) return;

        var avgLine = avgMatch.Groups[1].Value;
        var numbers = Regex.Matches(avgLine, @"(\d+\.?\d*)");

        if (numbers.Count < 4) return; // Need at least a few numbers

        var values = numbers.Select(m =>
        {
            decimal.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v);
            return v;
        }).ToList();

        // Try to assign values based on typical HVI column order
        // Common order: Mic, Len/UHML, Unif/UI, Str/GPT, Rd, +b, Leaf
        // Only fill in values that weren't already found by specific patterns
        for (int i = 0; i < values.Count; i++)
        {
            var v = values[i];
            if (!result.Micronaire.HasValue && v >= 2.0m && v <= 7.0m)
            {
                result.Micronaire = v;
            }
            else if (!result.Length.HasValue && v >= 20m && v <= 40m) // mm range
            {
                result.Length = v;
            }
            else if (!result.Length.HasValue && v >= 90m && v <= 140m) // hundredths of inch range
            {
                result.Length = v;
            }
            else if (!result.Uniformity.HasValue && v >= 70m && v <= 95m)
            {
                result.Uniformity = v;
            }
            else if (!result.StrengthGPT.HasValue && v >= 20m && v <= 45m)
            {
                result.StrengthGPT = v;
            }
            else if (!result.ColorRd.HasValue && v >= 50m && v <= 90m)
            {
                result.ColorRd = v;
            }
            else if (!result.Leaf.HasValue && v >= 1m && v <= 8m && v != result.Micronaire)
            {
                result.Leaf = v;
            }
        }
    }

    /// <summary>
    /// TesseractEngine expects the PARENT directory of 'tessdata'.
    /// e.g., if eng.traineddata is at /usr/share/tessdata/eng.traineddata,
    /// we return /usr/share (engine appends /tessdata/ internally).
    /// </summary>
    private static string? FindTessDataPath()
    {
        // TESSDATA_PREFIX env: could be the tessdata dir or its parent
        var envPath = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
        if (!string.IsNullOrEmpty(envPath))
        {
            // If TESSDATA_PREFIX IS the tessdata folder containing eng.traineddata
            if (File.Exists(Path.Combine(envPath, "eng.traineddata")))
                return Path.GetDirectoryName(envPath); // return parent
            // If TESSDATA_PREFIX is the parent that contains tessdata/
            if (File.Exists(Path.Combine(envPath, "tessdata", "eng.traineddata")))
                return envPath;
        }

        // tessdata directories to check (paths TO the tessdata folder)
        var tessdataDirs = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"),
            Path.Combine(Directory.GetCurrentDirectory(), "tessdata"),
            "/usr/share/tesseract-ocr/5/tessdata",
            "/usr/share/tesseract-ocr/4.00/tessdata",
            "/usr/share/tessdata",
            @"C:\Program Files\Tesseract-OCR\tessdata",
            @"C:\Program Files (x86)\Tesseract-OCR\tessdata",
        };

        foreach (var dir in tessdataDirs)
        {
            if (File.Exists(Path.Combine(dir, "eng.traineddata")))
                return Path.GetDirectoryName(dir); // return parent
        }

        return null;
    }
}
