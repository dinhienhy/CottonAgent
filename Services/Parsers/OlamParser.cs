using CBAS.Web.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CBAS.Web.Services.Parsers;

public class OlamParser : IShipperParser
{
    public string ShipperName => "Olam";

    public bool CanParse(string fileName, List<string> pdfRows)
    {
        if (fileName.Contains("olam", StringComparison.OrdinalIgnoreCase))
            return true;
        return pdfRows.Any(r =>
            r.Contains("Olam", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("olamagri.com", StringComparison.OrdinalIgnoreCase));
    }

    public OfferParseResult Parse(List<string> rows, int offerId)
    {
        var result = new OfferParseResult();
        string currentOrigin = "UNKNOWN";
        string currentCropYear = "";
        string? currentShipment = null;
        string? currentSettlement = null;

        // Extract ICE settlements and date
        foreach (var row in rows)
        {
            var iceMatch = Regex.Match(row, @"^(Jul|Dec|Mar|May)'\d{2}\s+(\d+\.\d+)\s+", RegexOptions.IgnoreCase);
            if (iceMatch.Success)
            {
                var fullMatch = Regex.Match(row, @"^([A-Za-z]+'\d{2})\s+(\d+\.\d+)");
                if (fullMatch.Success && decimal.TryParse(fullMatch.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                    result.ICESettlements[fullMatch.Groups[1].Value] = val;
            }

            var dateMatch = Regex.Match(row, @"Date:\s*(\d{1,2}-[A-Za-z]+-\d{2,4})", RegexOptions.IgnoreCase);
            if (dateMatch.Success && DateTime.TryParse(dateMatch.Groups[1].Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                result.OfferDate = DateTime.SpecifyKind(d, DateTimeKind.Utc);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i].Trim();
            if (string.IsNullOrWhiteSpace(row)) continue;

            // Skip non-data rows
            if (row.Contains("olamagri.com") || row.Contains("Cotton Market") ||
                row.Contains("Settlement") || row.Contains("ICE") ||
                row.Contains("We are pleased") || row.Contains("staple are determined") ||
                row.Contains("Kindly forward") || row.Contains("Best Regards") ||
                row.Contains("Olam Global") || row.Contains("7 Straits") ||
                row.Contains("Tel:") || row.Contains("SPECIAL OFFERS"))
                continue;

            // Detect section headers with origin + crop + shipment + settlement
            var sectionMatch = DetectSectionHeader(row);
            if (sectionMatch != null)
            {
                currentOrigin = sectionMatch.Value.origin;
                currentCropYear = sectionMatch.Value.cropYear;
                currentShipment = sectionMatch.Value.shipment;
                currentSettlement = sectionMatch.Value.settlement;
                continue;
            }

            // Skip header rows
            if (row.StartsWith("Quantity", StringComparison.OrdinalIgnoreCase)) continue;

            // Try parse Olam offer lines
            var lot = TryParseOlamLine(row, offerId, currentOrigin, currentCropYear, currentShipment, currentSettlement);
            if (lot != null)
                result.Lots.Add(lot);
        }

        return result;
    }

    private (string origin, string cropYear, string? shipment, string? settlement)? DetectSectionHeader(string row)
    {
        // Patterns:
        // "Brazil 2025 Crop Afloat lots (ETA are subject to changes without prior notice)"
        // "Australia 2026 Crop - BCI on request Prompt On Call Jul'26"
        // "Australia 2026 Crop - Physical BCI on request May/Jul'26 On Call Jul'26"
        // "Brazil (BCI) 2025 Crop - Physical BCI / regenagri on request May/Jun'26, S.O. On Call Jul'26"
        // "US OFFERS - BCI on request"
        // "M/E 25/26 May'26 On Call"
        // "West Africa 2025/26 Crop (CmiA excluding Mali & Senegal) May/Jul'26 On Call Jul'26"
        // "Mexico CH 25/26 May/Jun'26, S.O. On Call"
        // "EMOT 25/26 May'26 On Call"
        // "C/A 25/26 May'26 On Call"

        string? origin = null;
        string cropYear = "";
        string? shipment = null;
        string? settlement = null;

        // Check for origin keywords
        if (row.StartsWith("Brazil", StringComparison.OrdinalIgnoreCase)) origin = "BRAZIL";
        else if (row.StartsWith("Australia", StringComparison.OrdinalIgnoreCase)) origin = "AUSTRALIA";
        else if (row.StartsWith("US ", StringComparison.OrdinalIgnoreCase) || row.StartsWith("US OFFERS", StringComparison.OrdinalIgnoreCase)) origin = "USA";
        else if (row.StartsWith("West Africa", StringComparison.OrdinalIgnoreCase)) origin = "W.AFRICA";
        else if (row.StartsWith("Mexico", StringComparison.OrdinalIgnoreCase)) origin = "MEXICO";
        else if (row.StartsWith("Chad", StringComparison.OrdinalIgnoreCase)) origin = "CHAD";
        else if (row.StartsWith("Cameroon", StringComparison.OrdinalIgnoreCase)) origin = "CAMEROON";
        else if (row.StartsWith("Burkina", StringComparison.OrdinalIgnoreCase)) origin = "BURKINA FASO";
        else if (row.StartsWith("Benin", StringComparison.OrdinalIgnoreCase)) origin = "BENIN";
        else if (row.StartsWith("Mali", StringComparison.OrdinalIgnoreCase)) origin = "MALI";
        else if (row.StartsWith("Ivory", StringComparison.OrdinalIgnoreCase)) origin = "IVORY COAST";
        else if (row.StartsWith("Togo", StringComparison.OrdinalIgnoreCase)) origin = "TOGO";
        else if (row.StartsWith("Senegal", StringComparison.OrdinalIgnoreCase)) origin = "SENEGAL";

        // Sub-section headers for US (M/E, EMOT, C/A types)
        if (origin == null && Regex.IsMatch(row, @"^(M/E|EMOT|C/A)\s+\d{2}/\d{2}", RegexOptions.IgnoreCase))
            origin = "USA"; // US sub-section

        if (origin == null) return null;

        // Crop year
        var cropMatch = Regex.Match(row, @"(\d{4})\s+Crop|(\d{2}/\d{2})");
        if (cropMatch.Success)
            cropYear = cropMatch.Groups[1].Success ? cropMatch.Groups[1].Value : $"'{cropMatch.Groups[2].Value}";

        // Shipment
        var shipMatch = Regex.Match(row, @"((?:Prompt|(?:May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|Jan|Feb|Mar|Apr)(?:/[A-Za-z]+)?)'?\d{0,2},?\s*S\.?O\.?|(?:May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|Jan|Feb|Mar|Apr)(?:/[A-Za-z]+)?'?\d{2})", RegexOptions.IgnoreCase);
        if (shipMatch.Success)
            shipment = shipMatch.Value.Trim();

        // Settlement month (On Call XXX'YY)
        var settleMatch = Regex.Match(row, @"On\s+Call\s+([A-Za-z]+'\d{2})", RegexOptions.IgnoreCase);
        if (settleMatch.Success)
            settlement = settleMatch.Groups[1].Value;

        // Special: "Afloat lots" means prompt/ETA
        if (row.Contains("Afloat", StringComparison.OrdinalIgnoreCase))
            shipment = "Afloat";

        return (origin, cropYear, shipment, settlement);
    }

    private OfferLot? TryParseOlamLine(string row, int offerId, string origin, string cropYear, string? shipment, string? settlement)
    {
        // Pattern 1: SPECIAL OFFERS - Brazil Afloat with fixed price + ETA
        // "225 Brazil AF263 (Strict Middling) 1-1/8 Avg 3.9-4.9 (4.4 Avg) 30.3 Avg 92.75 5-May-26"
        var afloatMatch = Regex.Match(row,
            @"^(\d+)\s+(?:Brazil\s+)?(\w+)\s+\(([^)]+)\)\s+(\S+)\s+Avg\s+([\d.-]+)\s+\(([\d.]+)\s+Avg\)\s+([\d.]+)\s+Avg\s+([\d.]+)\s+(\S+)$",
            RegexOptions.IgnoreCase);
        if (afloatMatch.Success)
        {
            var qty = decimal.Parse(afloatMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var lotCode = afloatMatch.Groups[2].Value;
            var grade = afloatMatch.Groups[3].Value;
            var staple = afloatMatch.Groups[4].Value;
            var micAvg = afloatMatch.Groups[6].Value;
            var strAvg = afloatMatch.Groups[7].Value;
            var fixedPrice = decimal.Parse(afloatMatch.Groups[8].Value, CultureInfo.InvariantCulture);
            var eta = afloatMatch.Groups[9].Value;

            return new OfferLot
            {
                OfferId = offerId, LotCode = lotCode, Origin = origin,
                CropYear = cropYear, Type = grade, SpecialSpec = $"Stpl {staple}, Mic {micAvg}, Str {strAvg}",
                Quantity = qty, QuantityText = $"{qty} mt",
                OutrightPrice = fixedPrice, BasisCents = 0,
                PriceCentsPerLb = fixedPrice, SettlementMonth = settlement,
                ShipmentDateText = $"ETA {eta}", ShipmentDate = ParseEtaDate(eta),
                MicronaireSpec = micAvg, StrengthSpec = strAvg, LengthSpec = staple
            };
        }

        // Pattern 2: Recap lines with On Call basis (points)
        // "204 Recap 6DA0809 (Good Middling) 1-7/32 Avg 3.8-4.9 (4.6 Avg) 32.5 Avg 102.75 1850"
        var recapAvgMatch = Regex.Match(row,
            @"^(\d+)\s+Recap\s+(\S+)\s+\(([^)]+)\)\s+(\S+)\s+Avg\s+([\d.-]+)\s+\(([\d.]+)\s+Avg\)\s+([\d.]+)\s+Avg\s+([\d.]+)\s+(\d+)$",
            RegexOptions.IgnoreCase);
        if (recapAvgMatch.Success)
        {
            var qty = decimal.Parse(recapAvgMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var lotCode = recapAvgMatch.Groups[2].Value;
            var grade = recapAvgMatch.Groups[3].Value;
            var staple = recapAvgMatch.Groups[4].Value;
            var micAvg = recapAvgMatch.Groups[6].Value;
            var strAvg = recapAvgMatch.Groups[7].Value;
            var fixedPrice = decimal.Parse(recapAvgMatch.Groups[8].Value, CultureInfo.InvariantCulture);
            var basisPoints = int.Parse(recapAvgMatch.Groups[9].Value);

            return new OfferLot
            {
                OfferId = offerId, LotCode = lotCode, Origin = origin,
                CropYear = cropYear, Type = grade, SpecialSpec = $"Stpl {staple}, Mic {micAvg}, Str {strAvg}",
                Quantity = qty, QuantityText = $"{qty} mt",
                OutrightPrice = fixedPrice, BasisCents = basisPoints / 100m,
                PriceCentsPerLb = fixedPrice, SettlementMonth = settlement,
                ShipmentDateText = shipment, ShipmentDate = ParseShipmentText(shipment),
                MicronaireSpec = micAvg, StrengthSpec = strAvg, LengthSpec = staple
            };
        }

        // Pattern 3: US Recap lines  
        // "505 Recap M313389 (GC 31-3-38+, 4.5 Mic, 32 GPT) 1450 Jul'26"
        var usRecapMatch = Regex.Match(row,
            @"^(\d+)\s+Recap\s+(\S+)\s+\(([^)]+)\)\s+(\d+)\s+([A-Za-z]+'\d{2})$",
            RegexOptions.IgnoreCase);
        if (usRecapMatch.Success)
        {
            var qty = decimal.Parse(usRecapMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var lotCode = usRecapMatch.Groups[2].Value;
            var specInfo = usRecapMatch.Groups[3].Value;
            var basisPoints = int.Parse(usRecapMatch.Groups[4].Value);
            var settleMonth = usRecapMatch.Groups[5].Value;

            var (mic, str, staple) = ParseUSSpec(specInfo);

            return new OfferLot
            {
                OfferId = offerId, LotCode = lotCode, Origin = origin,
                CropYear = cropYear, Type = "M/E Recap",
                SpecialSpec = specInfo,
                Quantity = qty, QuantityText = $"{qty} mt",
                OutrightPrice = 0, BasisCents = basisPoints / 100m,
                PriceCentsPerLb = 0, SettlementMonth = settleMonth,
                ShipmentDateText = shipment, ShipmentDate = ParseShipmentText(shipment),
                MicronaireSpec = mic, StrengthSpec = str, LengthSpec = staple
            };
        }

        // Pattern 4: Generic description lines (no lot code)
        // "Strict Middling 1-3/16 G5 28 94.25 1000"
        // "500 GC 31-3-38, G5, 28 Min / 30 Min Avg 1450 Jul'26"
        // "500 SM 1-5/32", G5, 28 Min 1100 Jul'26"
        var genericOnCallMatch = Regex.Match(row,
            @"^(\d+)?\s*(.+?)\s+(\d{3,4})\s+([A-Za-z]+'\d{2})$");
        if (genericOnCallMatch.Success)
        {
            var qtyStr = genericOnCallMatch.Groups[1].Value;
            var spec = genericOnCallMatch.Groups[2].Value.Trim();
            var basisPoints = int.Parse(genericOnCallMatch.Groups[3].Value);
            var settleMonth = genericOnCallMatch.Groups[4].Value;

            // Skip if spec looks like a header/section
            if (spec.Contains("Crop") || spec.Contains("On Call") || spec.Contains("request"))
                return null;

            decimal qty = 0;
            if (!string.IsNullOrEmpty(qtyStr))
                decimal.TryParse(qtyStr, out qty);

            return new OfferLot
            {
                OfferId = offerId, LotCode = null, Origin = origin,
                CropYear = cropYear, Type = ExtractType(spec),
                SpecialSpec = spec,
                Quantity = qty, QuantityText = qty > 0 ? $"{qty} mt" : "Pls Inquire",
                OutrightPrice = 0, BasisCents = basisPoints / 100m,
                PriceCentsPerLb = 0, SettlementMonth = settleMonth,
                ShipmentDateText = shipment, ShipmentDate = ParseShipmentText(shipment)
            };
        }

        // Pattern 5: Description with fixed price + basis (West Africa etc)
        // "Chad A-51 1-3/16 G5 29 99.50 1525"
        // "BOLA-S 1-5/32 G5 29 99.00 1475"
        var fixedBasisMatch = Regex.Match(row,
            @"^(?:[A-Za-z]+\s+)?(.+?)\s+(\d+\.\d+)\s+(\d{3,4})$");
        if (fixedBasisMatch.Success)
        {
            var spec = fixedBasisMatch.Groups[1].Value.Trim();
            var fixedPrice = decimal.Parse(fixedBasisMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            var basisPoints = int.Parse(fixedBasisMatch.Groups[3].Value);

            if (spec.Contains("Crop") || spec.Contains("On Call") || spec.Contains("request") || spec.Contains("olamagri"))
                return null;

            return new OfferLot
            {
                OfferId = offerId, LotCode = null, Origin = origin,
                CropYear = cropYear, Type = ExtractType(spec),
                SpecialSpec = spec,
                Quantity = 0, QuantityText = "Pls Inquire",
                OutrightPrice = fixedPrice, BasisCents = basisPoints / 100m,
                PriceCentsPerLb = fixedPrice, SettlementMonth = settlement,
                ShipmentDateText = shipment, ShipmentDate = ParseShipmentText(shipment)
            };
        }

        return null;
    }

    private string ExtractType(string spec)
    {
        var match = Regex.Match(spec, @"^(Strict Middling|Middling|SLM|SM|M|GC|EMOT|T/\w+|[A-Z][\w-]+)");
        return match.Success ? match.Value : spec.Split(' ')[0];
    }

    private (string? mic, string? str, string? staple) ParseUSSpec(string specInfo)
    {
        string? mic = null, str = null, staple = null;
        var micMatch = Regex.Match(specInfo, @"([\d.]+)\s*Mic");
        if (micMatch.Success) mic = micMatch.Groups[1].Value;
        var strMatch = Regex.Match(specInfo, @"(\d+)\s*GPT");
        if (strMatch.Success) str = strMatch.Groups[1].Value;
        var stplMatch = Regex.Match(specInfo, @"(\d+-\d+-\d+)");
        if (stplMatch.Success) staple = stplMatch.Groups[1].Value;
        return (mic, str, staple);
    }

    private DateTime? ParseEtaDate(string eta)
    {
        if (DateTime.TryParse(eta, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return DateTime.SpecifyKind(d, DateTimeKind.Utc);
        return null;
    }

    private DateTime? ParseShipmentText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var match = Regex.Match(text, @"(May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|Jan|Feb|Mar|Apr)(?:/([A-Za-z]+))?'?(\d{2})?", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var monthStr = match.Groups[1].Value;
            var yearStr = match.Groups[3].Success ? match.Groups[3].Value : DateTime.UtcNow.Year.ToString().Substring(2);
            var months = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
                ["Jan"]=1,["Feb"]=2,["Mar"]=3,["Apr"]=4,["May"]=5,["Jun"]=6,
                ["Jul"]=7,["Aug"]=8,["Sep"]=9,["Oct"]=10,["Nov"]=11,["Dec"]=12
            };
            if (months.TryGetValue(monthStr[..3], out var m))
            {
                var y = 2000 + int.Parse(yearStr);
                return new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
            }
        }
        return null;
    }
}
