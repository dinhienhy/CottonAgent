using CBAS.Web.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CBAS.Web.Services.Parsers;

public class BrighannParser : IShipperParser
{
    public string ShipperName => "Brighann";

    public bool CanParse(string fileName, List<string> pdfRows)
    {
        if (fileName.Contains("Brighann", StringComparison.OrdinalIgnoreCase))
            return true;
        return pdfRows.Any(r =>
            r.Contains("Brighann", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("brighann.com", StringComparison.OrdinalIgnoreCase));
    }

    public OfferParseResult Parse(List<string> rows, int offerId)
    {
        var result = new OfferParseResult();
        string currentOrigin = "UNKNOWN";
        string currentCropYear = "";
        int seqNum = 0;

        // Pass 1: Extract ICE + date
        foreach (var row in rows)
        {
            var iceMatch = Regex.Match(row, @"^([A-Za-z]{3})-(\d{2})\s+(\d+\.\d+)", RegexOptions.IgnoreCase);
            if (iceMatch.Success && decimal.TryParse(iceMatch.Groups[3].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                result.ICESettlements[$"{iceMatch.Groups[1].Value}'{iceMatch.Groups[2].Value}"] = val;

            var dateMatch = Regex.Match(row, @"DAILY OFFERS\s+(\d{1,2}-[A-Za-z]+-\d{2,4})", RegexOptions.IgnoreCase);
            if (dateMatch.Success && DateTime.TryParse(dateMatch.Groups[1].Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                result.OfferDate = DateTime.SpecifyKind(d, DateTimeKind.Utc);
        }

        // Pass 2: Parse lots
        for (int i = 0; i < rows.Count; i++)
        {
            var rawRow = rows[i];
            var row = rawRow.Trim();
            if (string.IsNullOrWhiteSpace(row)) continue;

            // Skip header/footer/meta rows
            if (IsSkipRow(row)) continue;

            // ICE lines already processed
            if (Regex.IsMatch(row, @"^[A-Za-z]{3}-\d{2}\s+\d+\.\d+")) continue;

            // Origin
            if (row == "Australia") { currentOrigin = "AUSTRALIA"; continue; }
            if (row == "Brazil") { currentOrigin = "BRAZIL"; continue; }
            if (row == "USA") { currentOrigin = "USA"; continue; }
            if (row == "India") { currentOrigin = "INDIA"; continue; }

            // Crop year
            var cropMatch = Regex.Match(row, @"^(\d{4})\s+Crop$");
            if (cropMatch.Success) { currentCropYear = cropMatch.Groups[1].Value; continue; }

            // Data row: "SM 37 G5 29gpt CIF 500 2,200 Dec-26 16.75 97.21 Jul-Aug S.O."
            var lot = TryParseLine(row, offerId, currentOrigin, currentCropYear, ref seqNum);
            if (lot != null)
            {
                lot.SourceLineNumber = i;
                lot.SourceRawLine = rawRow.Trim();
                Console.WriteLine($"[Brighann] Parsed: {lot.LotCode} | {lot.Origin} | Basis={lot.BasisCents} | Fixed={lot.OutrightPrice} | Ship={lot.ShipmentDateText}");
                result.Lots.Add(lot);
            }
        }

        return result;
    }

    private bool IsSkipRow(string row)
    {
        var skips = new[] { "Brighann Cotton", "Moree", "marketing@", "Michael", "Ricky", "Neeraj",
            "DAILY OFFERS", "Terms", "Indicative", "ICA Rules", "Shipment (SO)", "Sight L/C",
            "Prices to be", "BCI +", "Vietnam C1", "Fixation", "Ports HCMC", "Location All",
            "CIF / EXW", "earlier", "Ph -", "Watercourse" };
        return skips.Any(s => row.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    private OfferLot? TryParseLine(string row, int offerId, string origin, string cropYear, ref int seq)
    {
        // "SM 37 G5 29gpt CIF 500 2,200 Dec-26 16.75 97.21 Jul-Aug S.O."
        // "EMOT 31-3-36 G5 CIF 500 2,200 Jul-26 11.50 90.70 May"
        var match = Regex.Match(row,
            @"^(.+?)\s+CIF\s+([\d,]+)\s+([\d,]+)\s+([A-Za-z]+-\d{2})\s+([\d.]+)\s+([\d.]+)\s+(.+)$");

        if (!match.Success) return null;

        var spec = match.Groups[1].Value.Trim();
        var qtyMt = decimal.Parse(match.Groups[2].Value.Replace(",", ""), CultureInfo.InvariantCulture);
        var bales = match.Groups[3].Value.Replace(",", "");
        var basisMonth = match.Groups[4].Value;
        var basisValue = decimal.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture);
        var fixedPrice = decimal.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture);
        var shipmentText = match.Groups[7].Value.Trim();

        // Parse spec components
        string? mic = null, str = null, staple = null;
        var gptM = Regex.Match(spec, @"(\d+)\s*gpt", RegexOptions.IgnoreCase);
        if (gptM.Success) { str = gptM.Groups[1].Value; spec = spec.Replace(gptM.Value, "").Trim(); }
        var micM = Regex.Match(spec, @"\bG(\d)\b");
        if (micM.Success) { mic = micM.Value; spec = spec.Replace(micM.Value, "").Trim(); }
        // Staple: "31-3-36" or "37" or "36"
        var stplM = Regex.Match(spec, @"\b(\d{1,2}-\d+-\d{2}|\d{2})\b");
        if (stplM.Success) { staple = stplM.Value; spec = spec.Replace(stplM.Value, "").Trim(); }
        var type = spec.Trim();
        if (string.IsNullOrEmpty(type)) type = "Cotton";

        // Generate LotCode: "BR-SM37-G5-001"
        seq++;
        var originCode = origin switch { "AUSTRALIA" => "AU", "BRAZIL" => "BR", "USA" => "US", _ => origin[..2] };
        var lotCode = $"{originCode}-{type.Replace(" ", "")}{staple ?? ""}-{seq:D3}";

        return new OfferLot
        {
            OfferId = offerId,
            LotCode = lotCode,
            Origin = origin,
            CropYear = cropYear,
            Type = type,
            SpecialSpec = $"{mic} {str}gpt".Trim(),
            Quantity = qtyMt,
            QuantityText = $"{qtyMt} mt ({bales} bales)",
            OutrightPrice = fixedPrice,
            BasisCents = basisValue,
            PriceCentsPerLb = fixedPrice,
            SettlementMonth = Regex.Replace(basisMonth, @"([A-Za-z]+)-(\d{2})", "$1'$2"),
            ShipmentDateText = shipmentText,
            ShipmentDate = ParseShipmentText(shipmentText),
            MicronaireSpec = mic,
            StrengthSpec = str,
            LengthSpec = staple
        };
    }

    private DateTime? ParseShipmentText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var months = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
            ["Jan"]=1,["Feb"]=2,["Mar"]=3,["Apr"]=4,["May"]=5,["Jun"]=6,
            ["Jul"]=7,["Aug"]=8,["Sep"]=9,["Oct"]=10,["Nov"]=11,["Dec"]=12
        };
        var match = Regex.Match(text, @"([A-Za-z]{3})");
        if (match.Success && months.TryGetValue(match.Value, out var m))
        {
            var year = DateTime.UtcNow.Year;
            if (m < DateTime.UtcNow.Month) year++;
            return new DateTime(year, m, 1, 0, 0, 0, DateTimeKind.Utc);
        }
        return null;
    }
}
