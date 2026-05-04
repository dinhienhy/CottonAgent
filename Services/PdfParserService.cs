using CBAS.Web.Models;
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
}

public class PdfParserService : IPdfParserService
{
    private readonly List<IShipperParser> _parsers;

    public PdfParserService()
    {
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
