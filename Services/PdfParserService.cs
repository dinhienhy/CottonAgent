using CBAS.Web.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text.RegularExpressions;
using System.Globalization;

namespace CBAS.Web.Services;

public class PdfParserService : IPdfParserService
{
    public async Task<List<OfferLot>> ParseOfferPdfAsync(Stream pdfStream, int offerId)
    {
        var lots = new List<OfferLot>();

        await Task.Run(() =>
        {
            using var document = PdfDocument.Open(pdfStream);
            
            foreach (var page in document.GetPages())
            {
                var text = page.Text;
                var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var lot = TryParseLotLine(line, offerId);
                    if (lot != null)
                    {
                        lots.Add(lot);
                    }
                }
            }
        });

        return lots;
    }

    public async Task<HVIReport?> ParseHVIPdfAsync(Stream pdfStream, string fileName)
    {
        HVIReport? report = null;

        await Task.Run(() =>
        {
            using var document = PdfDocument.Open(pdfStream);
            
            var allText = string.Empty;
            foreach (var page in document.GetPages())
            {
                allText += page.Text + "\n";
            }

            report = ParseHVIText(allText, fileName);
        });

        return report;
    }

    private OfferLot? TryParseLotLine(string line, int offerId)
    {
        var lotCodePattern = @"(ME\d{3}[A-Z]\d)";
        var match = Regex.Match(line, lotCodePattern);
        
        if (!match.Success)
            return null;

        var lot = new OfferLot
        {
            OfferId = offerId,
            LotCode = match.Groups[1].Value
        };

        var originPattern = @"(EGYPT|GREECE|TURKEY|USA|BRAZIL|AUSTRALIA|INDIA)";
        var originMatch = Regex.Match(line, originPattern, RegexOptions.IgnoreCase);
        if (originMatch.Success)
            lot.Origin = originMatch.Groups[1].Value.ToUpper();

        var cropYearPattern = @"(20\d{2}/\d{2}|20\d{2})";
        var cropYearMatch = Regex.Match(line, cropYearPattern);
        if (cropYearMatch.Success)
            lot.CropYear = cropYearMatch.Groups[1].Value;

        var quantityPattern = @"(\d+(?:\.\d+)?)\s*(?:MT|TONS|BALES)";
        var quantityMatch = Regex.Match(line, quantityPattern, RegexOptions.IgnoreCase);
        if (quantityMatch.Success && decimal.TryParse(quantityMatch.Groups[1].Value, out var qty))
            lot.Quantity = qty;

        var basisPattern = @"([+-]?\d+(?:\.\d+)?)\s*(?:pts|points)";
        var basisMatch = Regex.Match(line, basisPattern, RegexOptions.IgnoreCase);
        if (basisMatch.Success && decimal.TryParse(basisMatch.Groups[1].Value, out var basis))
            lot.BasisPoints = basis;

        var datePattern = @"(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})";
        var dateMatch = Regex.Match(line, datePattern);
        if (dateMatch.Success && DateTime.TryParse(dateMatch.Groups[1].Value, out var shipDate))
            lot.ShipmentDate = shipDate;

        return lot;
    }

    private HVIReport? ParseHVIText(string text, string fileName)
    {
        var lotCodePattern = @"(ME\d{3}[A-Z]\d)";
        var lotMatch = Regex.Match(text, lotCodePattern);
        
        if (!lotMatch.Success)
            return null;

        var report = new HVIReport
        {
            LotCode = lotMatch.Groups[1].Value,
            FileName = fileName,
            RawDataJson = text
        };

        var micPattern = @"(?:Mic|Micronaire)[:\s]+(\d+\.\d+)";
        var micMatch = Regex.Match(text, micPattern, RegexOptions.IgnoreCase);
        if (micMatch.Success && decimal.TryParse(micMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var mic))
            report.Micronaire = mic;

        var lengthPattern = @"(?:Length|Len)[:\s]+(\d+\.\d+)";
        var lengthMatch = Regex.Match(text, lengthPattern, RegexOptions.IgnoreCase);
        if (lengthMatch.Success && decimal.TryParse(lengthMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var len))
            report.Length = len;

        var strengthPattern = @"(?:Strength|Str|GPT)[:\s]+(\d+\.\d+)";
        var strengthMatch = Regex.Match(text, strengthPattern, RegexOptions.IgnoreCase);
        if (strengthMatch.Success && decimal.TryParse(strengthMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var str))
            report.StrengthGPT = str;

        var uniformityPattern = @"(?:Uniformity|Unif)[:\s]+(\d+\.\d+)";
        var uniformityMatch = Regex.Match(text, uniformityPattern, RegexOptions.IgnoreCase);
        if (uniformityMatch.Success && decimal.TryParse(uniformityMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var unif))
            report.Uniformity = unif;

        var colorRdPattern = @"(?:Rd|Color)[:\s]+(\d+\.\d+)";
        var colorRdMatch = Regex.Match(text, colorRdPattern, RegexOptions.IgnoreCase);
        if (colorRdMatch.Success && decimal.TryParse(colorRdMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rd))
            report.ColorRd = rd;

        var leafPattern = @"(?:Leaf|Lf)[:\s]+(\d+\.\d+)";
        var leafMatch = Regex.Match(text, leafPattern, RegexOptions.IgnoreCase);
        if (leafMatch.Success && decimal.TryParse(leafMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var leaf))
            report.Leaf = leaf;

        var cropYearPattern = @"(?:Crop|Year)[:\s]+(20\d{2})";
        var cropYearMatch = Regex.Match(text, cropYearPattern, RegexOptions.IgnoreCase);
        if (cropYearMatch.Success)
            report.CropYear = cropYearMatch.Groups[1].Value;

        var balesPattern = @"(\d+)\s*(?:bales|BALES)";
        var balesMatch = Regex.Match(text, balesPattern);
        if (balesMatch.Success && int.TryParse(balesMatch.Groups[1].Value, out var bales))
            report.TotalBales = bales;

        return report;
    }
}
