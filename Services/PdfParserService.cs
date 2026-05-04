using CBAS.Web.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text.Json;

namespace CBAS.Web.Services;

public class OfferParseResult
{
    public DateTime OfferDate { get; set; }
    public Dictionary<string, decimal> ICESettlements { get; set; } = new();
    public List<OfferLot> Lots { get; set; } = new();
}

public class PdfParserService : IPdfParserService
{
    private static readonly string[] OriginKeywords = {
        "U.S.A cotton", "Brazilian cotton", "Greek cotton",
        "Australian cotton", "Argentina cotton", "Mexican cotton",
        "Egyptian cotton", "Indian cotton", "Turkish cotton",
        "West African cotton", "CIS cotton", "Pakistan cotton"
    };

    private static readonly Dictionary<string, string> OriginMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["U.S.A cotton"] = "USA",
        ["Brazilian cotton"] = "BRAZIL",
        ["Greek cotton"] = "GREECE",
        ["Australian cotton"] = "AUSTRALIA",
        ["Argentina cotton"] = "ARGENTINA",
        ["Mexican cotton"] = "MEXICO",
        ["Egyptian cotton"] = "EGYPT",
        ["Indian cotton"] = "INDIA",
        ["Turkish cotton"] = "TURKEY",
        ["West African cotton"] = "W.AFRICA",
        ["CIS cotton"] = "CIS",
        ["Pakistan cotton"] = "PAKISTAN"
    };

    public async Task<List<OfferLot>> ParseOfferPdfAsync(Stream pdfStream, int offerId)
    {
        return await Task.Run(() =>
        {
            var result = ParseOfferPdfInternal(pdfStream, offerId);
            return result.Lots;
        });
    }

    public OfferParseResult ParseOfferPdfFull(Stream pdfStream, int offerId)
    {
        return ParseOfferPdfInternal(pdfStream, offerId);
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

    private OfferParseResult ParseOfferPdfInternal(Stream pdfStream, int offerId)
    {
        var result = new OfferParseResult();

        using var document = PdfDocument.Open(pdfStream);

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0) continue;

            var rows = words
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
                .OrderByDescending(g => g.Key)
                .Select(g => string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)))
                .ToList();

            ParseOfferRows(rows, result, offerId);
        }

        return result;
    }

    private void ParseOfferRows(List<string> rows, OfferParseResult result, int offerId)
    {
        string currentOrigin = "UNKNOWN";
        string? currentShipmentText = null;
        string currentCropYear = "";
        var pendingLots = new List<OfferLot>();

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i].Trim();
            if (string.IsNullOrWhiteSpace(row)) continue;

            // Parse offer date (format: M/D/YYYY)
            if (Regex.IsMatch(row, @"^\d{1,2}/\d{1,2}/\d{4}$"))
            {
                if (DateTime.TryParse(row, CultureInfo.InvariantCulture, DateTimeStyles.None, out var offerDate))
                    result.OfferDate = DateTime.SpecifyKind(offerDate, DateTimeKind.Utc);
                continue;
            }

            // Parse ICE settlement line: JUL'26 84.19 1.99
            var iceMatch = Regex.Match(row, @"^([A-Z]{3}'\d{2})\s+(\d+\.\d+)\s+");
            if (iceMatch.Success)
            {
                var month = iceMatch.Groups[1].Value;
                if (decimal.TryParse(iceMatch.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var settle))
                    result.ICESettlements[month] = settle;
                continue;
            }

            // Detect origin section header
            var originFound = false;
            foreach (var kw in OriginKeywords)
            {
                if (row.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    FlushPendingLots(pendingLots, currentShipmentText);
                    currentOrigin = OriginMap.GetValueOrDefault(kw, kw.Replace(" cotton", "").ToUpper());
                    currentShipmentText = null;
                    currentCropYear = "";
                    originFound = true;
                    break;
                }
            }
            if (originFound) continue;

            // Parse carrying charges
            if (Regex.IsMatch(row, @"Carrying\s+charges", RegexOptions.IgnoreCase))
                continue;

            // Skip header rows
            if (row.StartsWith("Q'tity", StringComparison.OrdinalIgnoreCase) ||
                row.StartsWith("ICE Cotton", StringComparison.OrdinalIgnoreCase) ||
                row.StartsWith("Dear ", StringComparison.OrdinalIgnoreCase) ||
                row.StartsWith("Please see", StringComparison.OrdinalIgnoreCase) ||
                row.StartsWith("These are all", StringComparison.OrdinalIgnoreCase) ||
                row.StartsWith("For Other", StringComparison.OrdinalIgnoreCase) ||
                row.Contains("Toyoshima") ||
                row.Contains("Nishiki") ||
                row.Contains("Tel (") ||
                row.Contains("http://") ||
                row.Contains("JAPAN Office") ||
                row.Contains("USA Office") ||
                row.Contains("Settle") ||
                row.Contains("Kazuki") ||
                row.Contains("Naoto") ||
                row.Contains("Hinana") ||
                row.Contains("Aichi-ken"))
                continue;

            // Parse shipment line: "Shipment 6/7'2026 SO"
            var shipMatch = Regex.Match(row, @"Shipment\s+(.+?)\s*$", RegexOptions.IgnoreCase);
            if (shipMatch.Success)
            {
                currentShipmentText = shipMatch.Groups[1].Value.Trim();
                FlushPendingLots(pendingLots, currentShipmentText);
                continue;
            }

            // Try to parse offer line
            var lot = TryParseOfferLine(row, offerId, currentOrigin);
            if (lot != null)
            {
                // Track crop year from generic lines for recap lines
                if (!string.IsNullOrEmpty(lot.CropYear))
                    currentCropYear = lot.CropYear;
                else if (!string.IsNullOrEmpty(currentCropYear))
                    lot.CropYear = currentCropYear;

                pendingLots.Add(lot);
            }
        }

        FlushPendingLots(pendingLots, currentShipmentText);
        result.Lots.AddRange(pendingLots);
    }

    private void FlushPendingLots(List<OfferLot> pendingLots, string? shipmentText)
    {
        if (string.IsNullOrEmpty(shipmentText)) return;

        foreach (var lot in pendingLots.Where(l => string.IsNullOrEmpty(l.ShipmentDateText)))
        {
            lot.ShipmentDateText = shipmentText;
            lot.ShipmentDate = ParseShipmentDate(shipmentText);
        }
    }

    private OfferLot? TryParseOfferLine(string row, int offerId, string origin)
    {
        // Pattern 1: M/E Recap line
        // "M/E Recap ME066M6 420 mt 95.19 11.00 onJUL'26"
        var recapMatch = Regex.Match(row,
            @"M/E\s+Recap\s+(\S+)\s+(\d+)\s*(mt|bc)\s+(\d+\.?\d*)\s+(\d+\.?\d*)\s+on([A-Z]{3}'\d{2})",
            RegexOptions.IgnoreCase);

        if (recapMatch.Success)
        {
            var lotCode = recapMatch.Groups[1].Value;
            var qty = decimal.Parse(recapMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            var unit = recapMatch.Groups[3].Value;
            var outright = decimal.Parse(recapMatch.Groups[4].Value, CultureInfo.InvariantCulture);
            var basis = decimal.Parse(recapMatch.Groups[5].Value, CultureInfo.InvariantCulture);
            var settlement = recapMatch.Groups[6].Value;

            return new OfferLot
            {
                OfferId = offerId,
                LotCode = lotCode,
                Origin = origin,
                Type = $"M/E Recap {lotCode}",
                SpecialSpec = "-",
                Quantity = qty,
                QuantityText = $"{qty} {unit}",
                OutrightPrice = outright,
                BasisCents = basis,
                PriceCentsPerLb = outright,
                SettlementMonth = settlement,
                CropYear = ParseCropYearFromContext(row)
            };
        }

        // Pattern 2: Generic offer line
        // "25/26 EMOT GC31336 G5 28GPT min PlsInquire mt 99.19 15.00 onJUL'26"
        // "2025 MID 1-5/32 G5 28GPT min PlsInquire mt 94.19 10.00 onJUL'26"
        // "2026 SM 37 G5 29 GPT min PlsInquire bc 102.06 17.50 onJUL'26"
        var genericMatch = Regex.Match(row,
            @"^(\d{2}/\d{2}|\d{4})\s+(.+?)\s+(PlsInquire|\d+)\s*(mt|bc)\s+(\d+\.?\d*)\s+(\d+\.?\d*)\s+on([A-Z]{3}'\d{2})",
            RegexOptions.IgnoreCase);

        if (genericMatch.Success)
        {
            var cropYear = genericMatch.Groups[1].Value;
            var specPart = genericMatch.Groups[2].Value.Trim();
            var qtyText = genericMatch.Groups[3].Value;
            var unit = genericMatch.Groups[4].Value;
            var outright = decimal.Parse(genericMatch.Groups[5].Value, CultureInfo.InvariantCulture);
            var basis = decimal.Parse(genericMatch.Groups[6].Value, CultureInfo.InvariantCulture);
            var settlement = genericMatch.Groups[7].Value;

            decimal qty = 0;
            string quantityDisplay = "Pls Inquire";
            if (decimal.TryParse(qtyText, out var parsedQty))
            {
                qty = parsedQty;
                quantityDisplay = $"{qty} {unit}";
            }

            var (type, specialSpec, colorSpec, leafSpec, lengthSpec, micSpec, strSpec) = ParseSpecPart(specPart);

            return new OfferLot
            {
                OfferId = offerId,
                LotCode = null,
                Origin = origin,
                CropYear = cropYear.Length == 4 ? cropYear : $"'{cropYear}",
                Type = type,
                SpecialSpec = specialSpec,
                Quantity = qty,
                QuantityText = quantityDisplay,
                OutrightPrice = outright,
                BasisCents = basis,
                PriceCentsPerLb = outright,
                SettlementMonth = settlement,
                ColorSpec = colorSpec,
                LeafSpec = leafSpec,
                LengthSpec = lengthSpec,
                MicronaireSpec = micSpec,
                StrengthSpec = strSpec
            };
        }

        return null;
    }

    private (string type, string? specialSpec, string? color, string? leaf, string? length, string? mic, string? strength) ParseSpecPart(string specPart)
    {
        // Examples:
        // "EMOT GC31336 G5 28GPT min" → type=EMOT, special=GC, color=31, leaf=3, length=36, mic=G5, str=28
        // "EMOT MID 1-1/8 G5 28GPT min" → type=EMOT, special=MID 1-1/8, mic=G5, str=28
        // "EMOT SLM 1-1/8 3.5/5.3NCL 28GPT min" → type=EMOT, special=SLM 1-1/8 3.5/5.3NCL, str=28
        // "MID 1-5/32 G5 28GPT min" → type=MID, special=1-5/32, mic=G5, str=28
        // "SM 37 G5 29 GPT min" → type=SM, special=37, mic=G5, str=29

        string type = specPart;
        string? specialSpec = null;
        string? colorSpec = null, leafSpec = null, lengthSpec = null, micSpec = null, strSpec = null;

        // Extract GPT/strength: "28GPT" or "29 GPT"
        var gptMatch = Regex.Match(specPart, @"(\d+)\s*GPT\s*min", RegexOptions.IgnoreCase);
        if (gptMatch.Success)
        {
            strSpec = gptMatch.Groups[1].Value;
            specPart = specPart.Substring(0, gptMatch.Index).Trim();
        }

        // Extract Micronaire grade: "G5"
        var micGradeMatch = Regex.Match(specPart, @"\bG(\d)\b");
        if (micGradeMatch.Success)
        {
            micSpec = micGradeMatch.Value;
            specPart = specPart.Replace(micGradeMatch.Value, "").Trim();
        }

        // Check for compressed spec code: GC31336, GC41436, etc.
        var compressedMatch = Regex.Match(specPart, @"^(\w+)\s+(GC|GM|SLM|MID|SM|LM)(\d{2})(\d)(\d{2})\b");
        if (compressedMatch.Success)
        {
            type = compressedMatch.Groups[1].Value;
            specialSpec = compressedMatch.Groups[2].Value;
            colorSpec = compressedMatch.Groups[3].Value;
            leafSpec = compressedMatch.Groups[4].Value;
            lengthSpec = compressedMatch.Groups[5].Value;
            return (type, specialSpec, colorSpec, leafSpec, lengthSpec, micSpec, strSpec);
        }

        // Also try pattern like: EMOT GC31336 (where GC is 2 chars + 5 digits)
        var compressedMatch2 = Regex.Match(specPart, @"^(\w+)\s+([A-Z]{2})(\d{1,2})(\d)(\d{2})\s*$");
        if (compressedMatch2.Success)
        {
            type = compressedMatch2.Groups[1].Value;
            specialSpec = compressedMatch2.Groups[2].Value;
            colorSpec = compressedMatch2.Groups[3].Value;
            leafSpec = compressedMatch2.Groups[4].Value;
            lengthSpec = compressedMatch2.Groups[5].Value;
            return (type, specialSpec, colorSpec, leafSpec, lengthSpec, micSpec, strSpec);
        }

        // General format: "TYPE [GRADE] [STAPLE] ..."
        var parts = specPart.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parts.Count >= 1)
        {
            type = parts[0];
            if (parts.Count > 1)
            {
                specialSpec = string.Join(" ", parts.Skip(1));
            }
        }

        return (type, specialSpec, colorSpec, leafSpec, lengthSpec, micSpec, strSpec);
    }

    private string ParseCropYearFromContext(string row)
    {
        var match = Regex.Match(row, @"(\d{2}/\d{2}|\d{4})");
        return match.Success ? (match.Value.Length == 4 ? match.Value : $"'{match.Value}") : "";
    }

    private DateTime? ParseShipmentDate(string shipmentText)
    {
        // "6/7'2026 SO" → extract months and year
        var match = Regex.Match(shipmentText, @"(\d{1,2})/(\d{1,2})'?(\d{4})");
        if (match.Success)
        {
            var month1 = int.Parse(match.Groups[1].Value);
            var year = int.Parse(match.Groups[3].Value);
            try
            {
                return new DateTime(year, month1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            catch { }
        }

        // "Prompt'2026 SO"
        var promptMatch = Regex.Match(shipmentText, @"Prompt'?(\d{4})");
        if (promptMatch.Success)
        {
            var year = int.Parse(promptMatch.Groups[1].Value);
            return new DateTime(year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        return null;
    }
}
