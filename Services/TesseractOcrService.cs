using CBAS.Web.DTOs;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Tesseract;
using UglyToad.PdfPig;

namespace CBAS.Web.Services;

public class TesseractOcrService : IOcrService
{
    private readonly string? _tessDataPath;
    private readonly ILogger<TesseractOcrService> _logger;

    public bool IsAvailable => _tessDataPath != null;

    public TesseractOcrService(ILogger<TesseractOcrService> logger)
    {
        _logger = logger;
        _tessDataPath = FindTessDataPath();

        if (_tessDataPath != null)
            _logger.LogInformation("Tesseract OCR ready. tessdata: {Path}", _tessDataPath);
        else
            _logger.LogWarning("Tesseract OCR not available - tessdata not found. Manual input only.");
    }

    public async Task<OcrResult> ProcessHVIPdfAsync(Stream pdfStream, string fileName)
    {
        if (!IsAvailable)
        {
            return new OcrResult
            {
                Success = false,
                ErrorMessage = "OCR không khả dụng (tessdata chưa cài đặt)"
            };
        }

        return await Task.Run(() =>
        {
            try
            {
                // Step 1: Extract images from PDF using PdfPig
                var imageBytes = ExtractImagesFromPdf(pdfStream);

                if (imageBytes.Count == 0)
                {
                    return new OcrResult
                    {
                        Success = false,
                        ErrorMessage = "Không tìm thấy hình ảnh trong PDF"
                    };
                }

                // Step 2: Run Tesseract OCR on each image
                var allText = new StringBuilder();
                float totalConfidence = 0;
                int pageCount = 0;

                _logger.LogInformation("Initializing Tesseract engine with tessdata path: {Path}", _tessDataPath);
                TesseractEngine engine;
                try
                {
                    engine = new TesseractEngine(_tessDataPath!, "eng", EngineMode.Default);
                    engine.SetVariable("preserve_interword_spaces", "1");
                }
                catch (Exception initEx)
                {
                    var initInner = initEx.InnerException?.Message ?? initEx.Message;
                    _logger.LogError(initEx, "Tesseract engine init failed: {Msg}", initInner);
                    return new OcrResult
                    {
                        Success = false,
                        ErrorMessage = $"Tesseract engine không khởi tạo được: {initInner}"
                    };
                }

                foreach (var imgBytes in imageBytes)
                {
                    try
                    {
                        using var pix = Pix.LoadFromMemory(imgBytes);
                        using var page = engine.Process(pix, PageSegMode.Auto);

                        var text = page.GetText();
                        var confidence = page.GetMeanConfidence();

                        allText.AppendLine(text);
                        totalConfidence += confidence;
                        pageCount++;

                        _logger.LogInformation("OCR page {Page}: {Chars} chars, confidence {Conf:P0}",
                            pageCount, text.Length, confidence);
                    }
                    catch (Exception ex)
                    {
                        var inner = ex.InnerException?.Message ?? ex.Message;
                        _logger.LogWarning("OCR failed on image ({Size} bytes): {Error} | Inner: {Inner}",
                            imgBytes.Length, ex.Message, inner);
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

                engine.Dispose();

                var rawText = allText.ToString();
                var avgConfidence = pageCount > 0 ? totalConfidence / pageCount : 0;

                // Step 3: Parse HVI fields from OCR text
                var result = ParseHVIFromOcrText(rawText);
                result.Success = true;
                result.Confidence = avgConfidence;
                result.RawText = rawText;

                _logger.LogInformation("OCR done for {File}: confidence={Conf:P0}, mic={Mic}, len={Len}, str={Str}",
                    fileName, avgConfidence, result.Micronaire, result.Length, result.StrengthGPT);

                return result;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "OCR error for {File}: {Inner}", fileName, innerMsg);
                return new OcrResult
                {
                    Success = false,
                    ErrorMessage = $"Lỗi OCR: {innerMsg}"
                };
            }
        });
    }

    private List<byte[]> ExtractImagesFromPdf(Stream pdfStream)
    {
        var images = new List<byte[]>();
        try
        {
            pdfStream.Position = 0;
            var pdfBytes = new byte[pdfStream.Length];
            pdfStream.Read(pdfBytes, 0, pdfBytes.Length);

            using var document = PdfDocument.Open(pdfBytes);
            foreach (var page in document.GetPages())
            {
                foreach (var image in page.GetImages())
                {
                    try
                    {
                        // TryGetPng decodes the PDF-internal format to standard PNG
                        if (image.TryGetPng(out var pngBytes) && pngBytes.Length > 1000)
                        {
                            images.Add(pngBytes);
                            _logger.LogInformation("Extracted PNG image: {Size} bytes, {W}x{H}",
                                pngBytes.Length, image.WidthInSamples, image.HeightInSamples);
                        }
                        else
                        {
                            // Fallback: try raw bytes (works for embedded JPEG)
                            var rawBytes = image.RawBytes.ToArray();
                            if (rawBytes.Length > 1000)
                            {
                                images.Add(rawBytes);
                                _logger.LogInformation("Extracted raw image: {Size} bytes, {W}x{H}",
                                    rawBytes.Length, image.WidthInSamples, image.HeightInSamples);
                            }
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

        return images;
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
