using CBAS.Web.Models;
using CBAS.Web.DTOs;
using CBAS.Web.Services.Parsers;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text.Json;

namespace CBAS.Web.Services;

public class OfferParseResult
{
    public string DetectedShipper { get; set; } = "Unknown";
    public DateTime OfferDate { get; set; }
    public Dictionary<string, decimal> ICESettlements { get; set; } = new();
    public List<OfferLot> Lots { get; set; } = new();
    public string? RawText { get; set; }
    public ClaudeParseLog? AILog { get; set; }
}

public class PdfParserService : IPdfParserService
{
    private readonly List<IShipperParser> _parsers;
    private readonly IClaudeParserService _claudeParser;

    public PdfParserService(IClaudeParserService claudeParser)
    {
        _claudeParser = claudeParser;
        _parsers = new List<IShipperParser>
        {
            new ToyoshimaParser(),
            new OlamParser(),
            new BrighannParser()
        };
    }

    public async Task<List<OfferLot>> ParseOfferPdfAsync(Stream pdfStream, int offerId)
    {
        return await Task.Run(() =>
        {
            var result = ParseOfferPdfInternal(pdfStream, offerId, "unknown.pdf");
            return result.Lots;
        });
    }

    public OfferParseResult ParseOfferPdfFull(Stream pdfStream, int offerId, string fileName = "unknown.pdf")
    {
        return ParseOfferPdfInternal(pdfStream, offerId, fileName);
    }

    public async Task<HVIReport?> ParseHVIPdfAsync(Stream pdfStream, string fileName)
    {
        return await Task.Run(() =>
        {
            var lotCode = ExtractLotCodeFromFileName(fileName);
            if (string.IsNullOrEmpty(lotCode))
                return null;

            var report = new HVIReport
            {
                LotCode = lotCode,
                FileName = fileName,
                RawDataJson = "{\"source\": \"filename_only\", \"note\": \"Scanned image PDF - HVI data must be entered manually\"}"
            };

            using var document = PdfDocument.Open(pdfStream);
            foreach (var page in document.GetPages())
            {
                var words = page.GetWords().ToList();
                if (words.Count > 0)
                {
                    var allText = string.Join(" ", words.Select(w => w.Text));
                    report.RawDataJson = allText;
                    ParseHVIFromText(report, allText);
                }
            }

            return report;
        });
    }

    private string? ExtractLotCodeFromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var match = Regex.Match(name, @"M[A-Z]?\d{3}[A-Z]\d", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Value.ToUpper();

        match = Regex.Match(name, @"[A-Z]{1,2}\d{3}[A-Z]\d", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Value.ToUpper();

        return name.Trim().ToUpper();
    }

    private void ParseHVIFromText(HVIReport report, string text)
    {
        var micMatch = Regex.Match(text, @"(?:Mic|Micronaire)[:\s]+(\d+\.?\d*)", RegexOptions.IgnoreCase);
        if (micMatch.Success && decimal.TryParse(micMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var mic))
            report.Micronaire = mic;

        var lenMatch = Regex.Match(text, @"(?:Length|Len|Staple)[:\s]+(\d+\.?\d*)", RegexOptions.IgnoreCase);
        if (lenMatch.Success && decimal.TryParse(lenMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var len))
            report.Length = len;

        var strMatch = Regex.Match(text, @"(?:Strength|Str|GPT)[:\s]+(\d+\.?\d*)", RegexOptions.IgnoreCase);
        if (strMatch.Success && decimal.TryParse(strMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var str))
            report.StrengthGPT = str;

        var unifMatch = Regex.Match(text, @"(?:Uniformity|Unif)[:\s]+(\d+\.?\d*)", RegexOptions.IgnoreCase);
        if (unifMatch.Success && decimal.TryParse(unifMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var unif))
            report.Uniformity = unif;

        var rdMatch = Regex.Match(text, @"(?:Rd|Color\s*Rd)[:\s]+(\d+\.?\d*)", RegexOptions.IgnoreCase);
        if (rdMatch.Success && decimal.TryParse(rdMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rd))
            report.ColorRd = rd;

        var leafMatch = Regex.Match(text, @"(?:Leaf|Lf)[:\s]+(\d+\.?\d*)", RegexOptions.IgnoreCase);
        if (leafMatch.Success && decimal.TryParse(leafMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var leaf))
            report.Leaf = leaf;
    }

    public async Task<OfferParseResult> ParseOfferPdfWithAIAsync(Stream pdfStream, int offerId, string fileName, int? shipperId)
    {
        var rawText = ExtractTextFromPdf(pdfStream);

        // AI primary: try Claude first
        if (_claudeParser.IsAvailable)
        {
            try
            {
                Console.WriteLine($"[AI Parser] Attempting Claude parse for: {fileName}");
                var (aiResult, log) = await _claudeParser.ParseOfferWithLogAsync(rawText, shipperId);
                if (aiResult != null && aiResult.Lots.Count > 0)
                {
                    Console.WriteLine($"[AI Parser] Success: {aiResult.Lots.Count} lots from {aiResult.Shipper}");
                    var result = ConvertAIResult(aiResult, offerId, rawText);
                    result.AILog = log;
                    return result;
                }
                // AI failed or no lots
                log.FellBackToRegex = true;
                log.AddStep("Falling back to regex parser");
                Console.WriteLine("[AI Parser] No lots returned, falling back to regex");
                pdfStream.Position = 0;
                var fallbackResult = ParseOfferPdfInternal(pdfStream, offerId, fileName);
                fallbackResult.AILog = log;
                return fallbackResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI Parser] Error: {ex.Message}, falling back to regex");
                var errorLog = new ClaudeParseLog
                {
                    Used = true, Status = "error", ErrorMessage = ex.Message, FellBackToRegex = true
                };
                errorLog.AddStep($"EXCEPTION: {ex.Message}");
                errorLog.AddStep("Falling back to regex parser");
                pdfStream.Position = 0;
                var fallbackResult = ParseOfferPdfInternal(pdfStream, offerId, fileName);
                fallbackResult.AILog = errorLog;
                return fallbackResult;
            }
        }

        // No AI available
        var noAiLog = new ClaudeParseLog { Used = false, Status = "idle" };
        noAiLog.AddStep("AI Parser not available (no API key). Using regex only.");
        pdfStream.Position = 0;
        var regexResult = ParseOfferPdfInternal(pdfStream, offerId, fileName);
        regexResult.AILog = noAiLog;
        return regexResult;
    }

    private string ExtractTextFromPdf(Stream pdfStream)
    {
        using var document = PdfDocument.Open(pdfStream);
        var allRows = new List<string>();
        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0) continue;
            var rows = words
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
                .OrderByDescending(g => g.Key)
                .Select(g => string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));
            allRows.AddRange(rows);
        }
        return string.Join("\n", allRows);
    }

    private OfferParseResult ConvertAIResult(ClaudeOfferResponse aiResult, int offerId, string rawText)
    {
        var result = new OfferParseResult
        {
            DetectedShipper = aiResult.Shipper,
            RawText = rawText
        };

        if (aiResult.IceJul26.HasValue)
            result.ICESettlements["JUL'26"] = aiResult.IceJul26.Value;

        if (!string.IsNullOrEmpty(aiResult.OfferDate) &&
            DateTime.TryParse(aiResult.OfferDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            result.OfferDate = DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        int autoIdx = 1;
        foreach (var lot in aiResult.Lots)
        {
            // Auto-generate LotCode if AI didn't extract one
            var lotCode = lot.KieuBong;
            if (string.IsNullOrWhiteSpace(lotCode))
            {
                var prefix = (lot.LoaiBong ?? aiResult.Shipper ?? "LOT").Replace(" ", "");
                if (prefix.Length > 8) prefix = prefix[..8];
                lotCode = $"{prefix}-{autoIdx:D3}";
                autoIdx++;
            }

            var offerLot = new OfferLot
            {
                OfferId = offerId,
                LotCode = lotCode ?? $"AUTO-{autoIdx++}",
                Origin = lot.LoaiBong ?? aiResult.Shipper,
                Quantity = lot.QuantityTan,
                Type = lot.TypeAllBci ?? lot.LoaiBong ?? string.Empty,
                SpecialSpec = BuildSpecialSpec(lot),
                ColorSpec = lot.MauSacColorGrade,
                LeafSpec = lot.TapLeaf,
                LengthSpec = lot.StapleChieuDai,
                MicronaireSpec = lot.Micronaire,
                StrengthSpec = lot.StrGptCuongLuc,
                BasisCents = lot.Basis ?? 0,
                SettlementMonth = lot.FutureMonth,
                OutrightPrice = lot.FixPriceBasis ?? 0,
                ShipmentDateText = lot.ShipmentGiaoHang,
            };

            // Calculate price if basis available
            if (lot.Basis.HasValue && aiResult.IceJul26.HasValue)
            {
                offerLot.PriceCentsPerLb = aiResult.IceJul26.Value + lot.Basis.Value;
            }
            else if (lot.FixPriceBasis.HasValue)
            {
                offerLot.PriceCentsPerLb = lot.FixPriceBasis.Value;
            }

            result.Lots.Add(offerLot);
        }

        return result;
    }

    private static string? BuildSpecialSpec(ClaudeOfferLot lot)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(lot.CapBongGrade)) parts.Add(lot.CapBongGrade);
        if (!string.IsNullOrEmpty(lot.KieuBong)) parts.Add(lot.KieuBong);
        if (!string.IsNullOrEmpty(lot.TypeAllBci)) parts.Add(lot.TypeAllBci);
        return parts.Count > 0 ? string.Join(" | ", parts) : null;
    }

    private OfferParseResult ParseOfferPdfInternal(Stream pdfStream, int offerId, string fileName)
    {
        using var document = PdfDocument.Open(pdfStream);

        var allRows = new List<string>();
        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0) continue;

            var rows = words
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
                .OrderByDescending(g => g.Key)
                .Select(g => string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)))
                .ToList();

            allRows.AddRange(rows);
        }

        // Detect shipper and route to correct parser
        var parser = _parsers.FirstOrDefault(p => p.CanParse(fileName, allRows));
        if (parser != null)
        {
            Console.WriteLine($"[Parser] Detected shipper: {parser.ShipperName} (file: {fileName})");
            var result = parser.Parse(allRows, offerId);
            result.DetectedShipper = parser.ShipperName;
            result.RawText = string.Join("\n", allRows);
            return result;
        }

        // Fallback: unknown shipper — log and return empty
        Console.WriteLine($"[Parser] WARNING: Could not detect shipper for file: {fileName}");
        return new OfferParseResult
        {
            DetectedShipper = "Unknown",
            RawText = string.Join("\n", allRows)
        };
    }
}
