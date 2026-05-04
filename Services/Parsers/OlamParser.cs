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
        string currentType = "";
        int seq = 0;

        // Pass 1: ICE + date
        foreach (var row in rows)
        {
            var iceMatch = Regex.Match(row, @"^([A-Za-z]+'\d{2})\s+(\d+\.\d+)\s+[\d.-]+");
            if (iceMatch.Success && decimal.TryParse(iceMatch.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                result.ICESettlements[iceMatch.Groups[1].Value] = val;

            var dateMatch = Regex.Match(row, @"Date:\s*(\d{1,2}-[A-Za-z]+-\d{2,4})", RegexOptions.IgnoreCase);
            if (dateMatch.Success && DateTime.TryParse(dateMatch.Groups[1].Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                result.OfferDate = DateTime.SpecifyKind(d, DateTimeKind.Utc);
        }

        // Pass 2: Parse lots
        foreach (var rawRow in rows)
        {
            var row = rawRow.Trim();
            if (string.IsNullOrWhiteSpace(row)) continue;
            if (IsSkipRow(row)) continue;

            // ICE lines already parsed
            if (Regex.IsMatch(row, @"^[A-Za-z]+'\d{2}\s+\d+\.\d+\s+")) continue;

            // Section header
            var hdr = DetectSectionHeader(row);
            if (hdr != null)
            {
                if (!string.IsNullOrEmpty(hdr.Value.origin)) currentOrigin = hdr.Value.origin;
                if (!string.IsNullOrEmpty(hdr.Value.cropYear)) currentCropYear = hdr.Value.cropYear;
                if (hdr.Value.shipment != null) currentShipment = hdr.Value.shipment;
                if (hdr.Value.settlement != null) currentSettlement = hdr.Value.settlement;
                if (!string.IsNullOrEmpty(hdr.Value.type)) currentType = hdr.Value.type;
                continue;
            }

            if (row.StartsWith("Quantity", StringComparison.OrdinalIgnoreCase)) continue;

            var lot = TryParseLine(row, offerId, currentOrigin, currentCropYear, currentShipment, currentSettlement, currentType, ref seq);
            if (lot != null)
            {
                Console.WriteLine($"[Olam] Parsed: {lot.LotCode} | {lot.Origin} | Mic={lot.MicronaireSpec} | Len={lot.LengthSpec} | Str={lot.StrengthSpec} | Basis={lot.BasisCents} | Fixed={lot.OutrightPrice}");
                result.Lots.Add(lot);
            }
        }

        return result;
    }

    private bool IsSkipRow(string row)
    {
        var skips = new[] { "olamagri.com", "Cotton Market Commentary", "Settlement Change",
            "ICE July", "ICE Cert", "Elsewhere", "Seminole", "Drought", "areas in", "rain since",
            "Traders report", "figures.", "We are pleased", "staple are determined",
            "Kindly forward", "Best Regards", "Olam Global", "7 Straits", "Tel:", "SPECIAL OFFERS",
            "including the", "session highs", "showing up" };
        return skips.Any(s => row.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    private (string? origin, string cropYear, string? shipment, string? settlement, string? type)? DetectSectionHeader(string row)
    {
        string? origin = null;
        string cropYear = "";
        string? shipment = null;
        string? settlement = null;
        string? type = null;

        // Country origins
        if (row.StartsWith("Brazil", StringComparison.OrdinalIgnoreCase) && (row.Contains("Crop") || row.Contains("BCI") || row.Contains("request"))) origin = "BRAZIL";
        else if (row.StartsWith("Australia", StringComparison.OrdinalIgnoreCase)) origin = "AUSTRALIA";
        else if (row.StartsWith("US ", StringComparison.OrdinalIgnoreCase) || row.StartsWith("US OFFERS", StringComparison.OrdinalIgnoreCase)) origin = "USA";
        else if (row.StartsWith("West Africa", StringComparison.OrdinalIgnoreCase)) origin = "W.AFRICA";
        else if (row.StartsWith("Mexico", StringComparison.OrdinalIgnoreCase) && (row.Contains("25/26") || row.Contains("Crop"))) origin = "MEXICO";
        // W.Africa sub-countries as section headers only if followed by crop/year info
        else if (Regex.IsMatch(row, @"^(Chad|Cameroon|Burkina|Benin|Mali|Ivory|Togo|Senegal)\b", RegexOptions.IgnoreCase) && !Regex.IsMatch(row, @"\d+\.\d+\s+\d{3,4}$"))
        {
            var m = Regex.Match(row, @"^(Chad|Cameroon|Burkina|Benin|Mali|Ivory|Togo|Senegal)", RegexOptions.IgnoreCase);
            origin = m.Value.ToUpper();
            if (origin == "IVORY") origin = "IVORY COAST";
            if (origin == "BURKINA") origin = "BURKINA FASO";
        }

        // US sub-section types: M/E, EMOT, C/A
        if (origin == null && Regex.IsMatch(row, @"^(M/E|EMOT|C/A)\s+", RegexOptions.IgnoreCase))
        {
            origin = "USA";
            type = Regex.Match(row, @"^(M/E|EMOT|C/A)").Value;
        }

        if (origin == null) return null;

        // Crop year
        var cropMatch = Regex.Match(row, @"(\d{4})\s+Crop|(\d{2}/\d{2})");
        if (cropMatch.Success)
            cropYear = cropMatch.Groups[1].Success ? cropMatch.Groups[1].Value : $"'{cropMatch.Groups[2].Value}";

        // Shipment
        var shipMatch = Regex.Match(row, @"(Prompt|(?:May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|Jan|Feb|Mar|Apr)(?:/[A-Za-z]+)?'?\d{2})", RegexOptions.IgnoreCase);
        if (shipMatch.Success) shipment = shipMatch.Value.Trim();

        // Settlement
        var settleMatch = Regex.Match(row, @"On\s+Call\s+([A-Za-z]+'\d{2})", RegexOptions.IgnoreCase);
        if (settleMatch.Success) settlement = settleMatch.Groups[1].Value;

        if (row.Contains("Afloat", StringComparison.OrdinalIgnoreCase)) shipment = "Afloat";

        return (origin, cropYear, shipment, settlement, type);
    }

    private OfferLot? TryParseLine(string row, int offerId, string origin, string cropYear,
        string? shipment, string? settlement, string currentType, ref int seq)
    {
        // P1: Afloat "225 Brazil AF263 (Strict Middling) 1-1/8 Avg 3.9-4.9 (4.4 Avg) 30.3 Avg 92.75 5-May-26"
        var m1 = Regex.Match(row,
            @"^(\d+)\s+(?:Brazil\s+)?(\w+)\s+\(([^)]+)\)\s+(\S+)\s+Avg\s+[\d.]+-[\d.]+\s+\(([\d.]+)\s+Avg\)\s+([\d.]+)\s+Avg\s+([\d.]+)\s+(\S+)$");
        if (m1.Success)
            return MakeLot(offerId, m1.Groups[2].Value, origin, cropYear, m1.Groups[3].Value,
                decimal.Parse(m1.Groups[1].Value, CultureInfo.InvariantCulture),
                m1.Groups[4].Value, m1.Groups[5].Value, m1.Groups[6].Value,
                decimal.Parse(m1.Groups[7].Value, CultureInfo.InvariantCulture), 0,
                settlement, $"ETA {m1.Groups[8].Value}", ParseEtaDate(m1.Groups[8].Value));

        // P2: Recap with Avg "204 Recap 6DA0809 (Good Middling) 1-7/32 Avg 3.8-4.9 (4.6 Avg) 32.5 Avg 102.75 1850"
        var m2 = Regex.Match(row,
            @"^(\d+)\s+Recap\s+(\S+)\s+\(([^)]+)\)\s+(\S+)\s+Avg\s+[\d.]+-[\d.]+\s+\(([\d.]+)\s+Avg\)\s+([\d.]+)\s+Avg\s+([\d.]+)\s+(\d+)$");
        if (m2.Success)
            return MakeLot(offerId, m2.Groups[2].Value, origin, cropYear, m2.Groups[3].Value,
                decimal.Parse(m2.Groups[1].Value, CultureInfo.InvariantCulture),
                m2.Groups[4].Value, m2.Groups[5].Value, m2.Groups[6].Value,
                decimal.Parse(m2.Groups[7].Value, CultureInfo.InvariantCulture),
                int.Parse(m2.Groups[8].Value) / 100m,
                settlement, shipment, ParseShipmentText(shipment));

        // P3: US/Mexico Recap "505 Recap M313389 (GC 31-3-38+, 4.5 Mic, 32 GPT) 1450 Jul'26"
        //     also "300 Recap NHS34_NP (SM, Stpl 35.55, Mic 4.28 Avg, Gpt 27.20 Avg) 900 Jul'26"
        var m3 = Regex.Match(row, @"^(\d+)\s+Recap\s+(\S+)\s+\(([^)]+)\)\s+(\d+)\s+([A-Za-z]+'\d{2})$");
        if (m3.Success)
        {
            var specInfo = m3.Groups[3].Value;
            var (mic, str, staple) = ParseSpecInfo(specInfo);
            return MakeLot(offerId, m3.Groups[2].Value, origin, cropYear, specInfo,
                decimal.Parse(m3.Groups[1].Value, CultureInfo.InvariantCulture),
                staple, mic, str, 0, int.Parse(m3.Groups[4].Value) / 100m,
                m3.Groups[5].Value, shipment, ParseShipmentText(shipment));
        }

        // P4: Named lot with parens + fixed + basis "500 T/APEX (EQ GM) 1-3/16 G5 29 101.75 1750"
        //     or without qty: "T/BEUT (EQ SM) 1-7/32 G5 29 101.75 1750"
        var m4 = Regex.Match(row, @"^(\d+)?\s*(T/\S+|[A-Z][\w/-]+)\s+\(([^)]+)\)\s+(\S+)\s+(G\d)\s+(\d+)\s+(\d+\.\d+)\s+(\d+)$");
        if (m4.Success)
        {
            decimal qty = 0;
            if (!string.IsNullOrEmpty(m4.Groups[1].Value)) decimal.TryParse(m4.Groups[1].Value, out qty);
            return MakeLot(offerId, m4.Groups[2].Value, origin, cropYear, m4.Groups[3].Value,
                qty, m4.Groups[4].Value, m4.Groups[5].Value, m4.Groups[6].Value,
                decimal.Parse(m4.Groups[7].Value, CultureInfo.InvariantCulture),
                int.Parse(m4.Groups[8].Value) / 100m,
                settlement, shipment, ParseShipmentText(shipment));
        }

        // P5: Generic on-call with settlement "500 GC 31-3-38, G5, 28 Min / 30 Min Avg 1450 Jul'26"
        var m5 = Regex.Match(row, @"^(\d+)?\s*(.+?)\s+(\d{3,4})\s+([A-Za-z]+'\d{2})$");
        if (m5.Success)
        {
            var spec = m5.Groups[2].Value.Trim();
            if (spec.Contains("Crop") || spec.Contains("On Call") || spec.Contains("request") || spec.Contains("regenagri")) return null;
            decimal qty = 0;
            if (!string.IsNullOrEmpty(m5.Groups[1].Value)) decimal.TryParse(m5.Groups[1].Value, out qty);
            var (mic, str, staple) = ParseSpecInfo(spec);
            seq++;
            var lotCode = $"OL-{origin[..Math.Min(2, origin.Length)]}-{seq:D3}";
            return new OfferLot
            {
                OfferId = offerId, LotCode = lotCode, Origin = origin, CropYear = cropYear,
                Type = currentType.Length > 0 ? currentType : ExtractType(spec), SpecialSpec = spec,
                Quantity = qty, QuantityText = qty > 0 ? $"{qty} mt" : "Pls Inquire",
                OutrightPrice = 0, BasisCents = int.Parse(m5.Groups[3].Value) / 100m,
                PriceCentsPerLb = 0, SettlementMonth = m5.Groups[4].Value,
                ShipmentDateText = shipment, ShipmentDate = ParseShipmentText(shipment),
                MicronaireSpec = mic, StrengthSpec = str, LengthSpec = staple
            };
        }

        // P6: Fixed price + basis (no settlement month) "BOLA-S 1-5/32 G5 29 99.00 1475"
        //     also "Strict Middling 1-3/16 G5 28 94.25 1000"
        //     also "200 T/COOLA (EQ SLM LS) 1-1/8 G5 28 91.25 700"
        var m6 = Regex.Match(row, @"^(\d+)?\s*(.+?)\s+(\d+\.\d+)\s+(\d{3,4})$");
        if (m6.Success)
        {
            var spec = m6.Groups[2].Value.Trim();
            if (spec.Contains("Crop") || spec.Contains("On Call") || spec.Contains("request") || spec.Contains("olamagri")) return null;
            decimal qty = 0;
            if (!string.IsNullOrEmpty(m6.Groups[1].Value)) decimal.TryParse(m6.Groups[1].Value, out qty);
            var fixedPrice = decimal.Parse(m6.Groups[3].Value, CultureInfo.InvariantCulture);
            var basisPts = int.Parse(m6.Groups[4].Value);
            var (mic, str, staple) = ParseSpecInfo(spec);

            // Try to extract lot name from spec
            string? lotName = null;
            var nameMatch = Regex.Match(spec, @"^([A-Z][\w/-]+(?:\s+\([^)]+\))?)");
            if (nameMatch.Success && !Regex.IsMatch(nameMatch.Value, @"^(Strict|Middling|SLM|SM|M\b)"))
                lotName = nameMatch.Value.Split(' ')[0];

            seq++;
            var lotCode = lotName ?? $"OL-{origin[..Math.Min(2, origin.Length)]}-{seq:D3}";

            return new OfferLot
            {
                OfferId = offerId, LotCode = lotCode, Origin = origin, CropYear = cropYear,
                Type = ExtractType(spec), SpecialSpec = spec,
                Quantity = qty, QuantityText = qty > 0 ? $"{qty} mt" : "Pls Inquire",
                OutrightPrice = fixedPrice, BasisCents = basisPts / 100m,
                PriceCentsPerLb = fixedPrice, SettlementMonth = settlement,
                ShipmentDateText = shipment, ShipmentDate = ParseShipmentText(shipment),
                MicronaireSpec = mic, StrengthSpec = str, LengthSpec = staple
            };
        }

        return null;
    }

    private OfferLot MakeLot(int offerId, string lotCode, string origin, string cropYear, string type,
        decimal qty, string? staple, string? mic, string? str,
        decimal fixedPrice, decimal basisCents, string? settlement, string? shipText, DateTime? shipDate)
    {
        return new OfferLot
        {
            OfferId = offerId, LotCode = lotCode, Origin = origin, CropYear = cropYear,
            Type = type, SpecialSpec = $"Stpl {staple}, Mic {mic}, Str {str}",
            Quantity = qty, QuantityText = qty > 0 ? $"{qty} mt" : "Pls Inquire",
            OutrightPrice = fixedPrice, BasisCents = basisCents,
            PriceCentsPerLb = fixedPrice > 0 ? fixedPrice : 0,
            SettlementMonth = settlement,
            ShipmentDateText = shipText, ShipmentDate = shipDate,
            MicronaireSpec = mic, StrengthSpec = str, LengthSpec = staple
        };
    }

    private string ExtractType(string spec)
    {
        var m = Regex.Match(spec, @"^(Strict Middling|Middling|SLM|SM|GC|EMOT|T/\w+|[A-Z][\w/-]+)");
        return m.Success ? m.Value : spec.Split(' ')[0];
    }

    private (string? mic, string? str, string? staple) ParseSpecInfo(string spec)
    {
        string? mic = null, str = null, staple = null;
        // Mic: "4.5 Mic" or "G5" or "G6" or "Mic 4.28 Avg"
        var micM = Regex.Match(spec, @"([\d.]+)\s*Mic|Mic\s*([\d.]+)");
        if (micM.Success) mic = micM.Groups[1].Success ? micM.Groups[1].Value : micM.Groups[2].Value;
        if (mic == null) { var gm = Regex.Match(spec, @"\bG(\d)\b"); if (gm.Success) mic = gm.Value; }
        // Str: "32 GPT" or "Gpt 27.20" or "28 Min"
        var strM = Regex.Match(spec, @"(\d+(?:\.\d+)?)\s*(?:GPT|Gpt)|(?:GPT|Gpt)\s*([\d.]+)");
        if (strM.Success) str = strM.Groups[1].Success ? strM.Groups[1].Value : strM.Groups[2].Value;
        if (str == null) { var minM = Regex.Match(spec, @"(\d+)\s+Min"); if (minM.Success) str = minM.Groups[1].Value; }
        // Also check standalone "29" or "28" at end of spec (after G5/G6)
        if (str == null) { var numM = Regex.Match(spec, @"G\d\s+(\d+)$"); if (numM.Success) str = numM.Groups[1].Value; }
        // Staple: "1-3/16" or "31-3-38" or "Stpl 35.55"
        var stplM = Regex.Match(spec, @"Stpl\s*([\d.]+)|(\d+-\d+/\d+|\d+-\d+-\d+[+]?)");
        if (stplM.Success) staple = stplM.Groups[1].Success ? stplM.Groups[1].Value : stplM.Groups[2].Value;
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
        var match = Regex.Match(text, @"(May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|Jan|Feb|Mar|Apr)(?:/[A-Za-z]+)?'?(\d{2})?", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var months = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
                ["Jan"]=1,["Feb"]=2,["Mar"]=3,["Apr"]=4,["May"]=5,["Jun"]=6,
                ["Jul"]=7,["Aug"]=8,["Sep"]=9,["Oct"]=10,["Nov"]=11,["Dec"]=12
            };
            if (months.TryGetValue(match.Groups[1].Value[..3], out var m))
            {
                var y = match.Groups[2].Success ? 2000 + int.Parse(match.Groups[2].Value) : DateTime.UtcNow.Year;
                return new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
            }
        }
        return null;
    }
}
