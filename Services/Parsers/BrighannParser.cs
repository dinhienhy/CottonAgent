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

        // Extract ICE settlements from header
        foreach (var row in rows)
        {
            var iceMatch = Regex.Match(row, @"^(Jul|Dec|Mar|May)-(\d{2})\s+(\d+\.\d+)", RegexOptions.IgnoreCase);
            if (iceMatch.Success)
            {
                var month = $"{iceMatch.Groups[1].Value}'{iceMatch.Groups[2].Value}";
                if (decimal.TryParse(iceMatch.Groups[3].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                    result.ICESettlements[month] = val;
            }

            var dateMatch = Regex.Match(row, @"DAILY OFFERS\s+(\d{1,2}-[A-Za-z]+-\d{2,4})", RegexOptions.IgnoreCase);
            if (dateMatch.Success && DateTime.TryParse(dateMatch.Groups[1].Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                result.OfferDate = DateTime.SpecifyKind(d, DateTimeKind.Utc);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i].Trim();
            if (string.IsNullOrWhiteSpace(row)) continue;

            // Skip non-data rows
            if (row.Contains("Brighann Cotton") || row.Contains("Moree") ||
                row.Contains("marketing@") || row.Contains("Michael") ||
                row.Contains("Ricky") || row.Contains("Neeraj") ||
                row.Contains("DAILY OFFERS") || row.Contains("Terms") ||
                row.Contains("Indicative") || row.Contains("ICA Rules") ||
                row.Contains("Shipment (SO)") || row.Contains("Sight L/C") ||
                row.Contains("Prices to be") || row.Contains("BCI +") ||
                row.Contains("Vietnam C1%") || row.Contains("Fixation") ||
                row.Contains("Ports") || row.Contains("Location") ||
                row.Contains("CIF / EXW") || row.Contains("earlier"))
                continue;

            // ICE settlement lines (already processed above)
            if (Regex.IsMatch(row, @"^(Jul|Dec|Mar|May)-\d{2}\s+\d+\.\d+"))
                continue;

            // Detect origin section
            if (row == "Australia") { currentOrigin = "AUSTRALIA"; continue; }
            if (row == "Brazil") { currentOrigin = "BRAZIL"; continue; }
            if (row == "USA") { currentOrigin = "USA"; continue; }
            if (row == "India") { currentOrigin = "INDIA"; continue; }

            // Crop year line
            if (Regex.IsMatch(row, @"^\d{4}\s+Crop$"))
            {
                currentCropYear = Regex.Match(row, @"\d{4}").Value;
                continue;
            }

            // Try to parse Brighann offer line
            // Format: "SM 37 G5 29gpt CIF 500 2,200 Dec-26 16.75 97.21 Jul-Aug S.O."
            var lot = TryParseBrighannLine(row, offerId, currentOrigin, currentCropYear);
            if (lot != null)
                result.Lots.Add(lot);
        }

        return result;
    }

    private OfferLot? TryParseBrighannLine(string row, int offerId, string origin, string cropYear)
    {
        // Pattern: "SM 37 G5 29gpt CIF 500 2,200 Dec-26 16.75 97.21 Jul-Aug S.O."
        // Pattern: "EMOT 31-3-36 G5 CIF 500 2,200 Jul-26 11.50 90.70 May"
        // Pattern: "M 37 G5 28gpt CIF 500 2,200 Dec-26 8.75 89.21 Sep-Oct S.O."
        // Pattern: "SLM 36 G5 28gpt CIF 500 2,200 Dec-26 7.75 88.21 Oct-Nov S.O."

        var match = Regex.Match(row,
            @"^(.+?)\s+CIF\s+([\d,]+)\s+([\d,]+)\s+([A-Za-z]+-\d{2})\s+(\d+\.?\d*)\s+(\d+\.?\d*)\s+(.+)$",
            RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var spec = match.Groups[1].Value.Trim();
            var qtyMt = decimal.Parse(match.Groups[2].Value.Replace(",", ""), CultureInfo.InvariantCulture);
            var bales = match.Groups[3].Value.Replace(",", "");
            var basisMonth = match.Groups[4].Value;
            var basisCents = decimal.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture);
            var fixedPrice = decimal.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture);
            var shipmentText = match.Groups[7].Value.Trim();

            var (type, specialSpec, mic, str, staple) = ParseBrighannSpec(spec);

            return new OfferLot
            {
                OfferId = offerId,
                LotCode = null,
                Origin = origin,
                CropYear = cropYear,
                Type = type,
                SpecialSpec = specialSpec,
                Quantity = qtyMt,
                QuantityText = $"{qtyMt} mt ({bales} bales)",
                OutrightPrice = fixedPrice,
                BasisCents = basisCents,
                PriceCentsPerLb = fixedPrice,
                SettlementMonth = ConvertBasisMonth(basisMonth),
                ShipmentDateText = shipmentText,
                ShipmentDate = ParseShipmentText(shipmentText),
                MicronaireSpec = mic,
                StrengthSpec = str,
                LengthSpec = staple
            };
        }

        return null;
    }

    private (string type, string? specialSpec, string? mic, string? str, string? staple) ParseBrighannSpec(string spec)
    {
        string type = spec;
        string? specialSpec = null, mic = null, str = null, staple = null;

        // Extract GPT: "29gpt" or "28gpt"
        var gptMatch = Regex.Match(spec, @"(\d+)\s*gpt", RegexOptions.IgnoreCase);
        if (gptMatch.Success)
        {
            str = gptMatch.Groups[1].Value;
            spec = spec.Replace(gptMatch.Value, "").Trim();
        }

        // Extract micronaire grade: "G5"
        var micMatch = Regex.Match(spec, @"\bG(\d)\b");
        if (micMatch.Success)
        {
            mic = micMatch.Value;
            spec = spec.Replace(micMatch.Value, "").Trim();
        }

        // Extract staple: "37", "36", "31-3-36"
        var stapleMatch = Regex.Match(spec, @"\b(\d{1,2}-\d-\d{2}|\d{2})\b");
        if (stapleMatch.Success)
        {
            staple = stapleMatch.Value;
            spec = spec.Replace(stapleMatch.Value, "").Trim();
        }

        // Type is what's left
        type = spec.Trim();
        if (string.IsNullOrEmpty(type)) type = "Cotton";

        return (type, specialSpec, mic, str, staple);
    }

    private string ConvertBasisMonth(string basisMonth)
    {
        // "Dec-26" → "Dec'26"
        var m = Regex.Match(basisMonth, @"([A-Za-z]+)-(\d{2})");
        return m.Success ? $"{m.Groups[1].Value}'{m.Groups[2].Value}" : basisMonth;
    }

    private DateTime? ParseShipmentText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var months = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
            ["Jan"]=1,["Feb"]=2,["Mar"]=3,["Apr"]=4,["May"]=5,["Jun"]=6,
            ["Jul"]=7,["Aug"]=8,["Sep"]=9,["Oct"]=10,["Nov"]=11,["Dec"]=12
        };

        // "Jul-Aug S.O." or "May" or "Sep-Oct S.O."
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
