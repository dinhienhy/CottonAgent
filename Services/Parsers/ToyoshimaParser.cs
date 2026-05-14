using CBAS.Web.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CBAS.Web.Services.Parsers;

public class ToyoshimaParser : IShipperParser
{
    public string ShipperName => "Toyoshima";

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

    public bool CanParse(string fileName, List<string> pdfRows)
    {
        if (fileName.Contains("Toyoshima", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Offer_FE", StringComparison.OrdinalIgnoreCase))
            return true;

        return pdfRows.Any(r =>
            r.Contains("Toyoshima", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Nishiki", StringComparison.OrdinalIgnoreCase));
    }

    public OfferParseResult Parse(List<string> rows, int offerId)
    {
        var result = new OfferParseResult();
        string currentOrigin = "UNKNOWN";
        string? currentShipmentText = null;
        string currentCropYear = "";
        var pendingLots = new List<OfferLot>();

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i].Trim();
            if (string.IsNullOrWhiteSpace(row)) continue;

            if (Regex.IsMatch(row, @"^\d{1,2}/\d{1,2}/\d{4}$"))
            {
                if (DateTime.TryParse(row, CultureInfo.InvariantCulture, DateTimeStyles.None, out var offerDate))
                    result.OfferDate = DateTime.SpecifyKind(offerDate, DateTimeKind.Utc);
                continue;
            }

            var iceMatch = Regex.Match(row, @"^([A-Z]{3}'\d{2})\s+(\d+\.\d+)\s+");
            if (iceMatch.Success)
            {
                var month = iceMatch.Groups[1].Value;
                if (decimal.TryParse(iceMatch.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var settle))
                    result.ICESettlements[month] = settle;
                continue;
            }

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

            if (Regex.IsMatch(row, @"Carrying\s+charges", RegexOptions.IgnoreCase)) continue;

            if (row.StartsWith("Q'tity", StringComparison.OrdinalIgnoreCase) ||
                row.StartsWith("ICE Cotton", StringComparison.OrdinalIgnoreCase) ||
                row.StartsWith("Dear ", StringComparison.OrdinalIgnoreCase) ||
                row.StartsWith("Please see", StringComparison.OrdinalIgnoreCase) ||
                row.StartsWith("These are all", StringComparison.OrdinalIgnoreCase) ||
                row.StartsWith("For Other", StringComparison.OrdinalIgnoreCase) ||
                row.Contains("Toyoshima") || row.Contains("Nishiki") ||
                row.Contains("Tel (") || row.Contains("http://") ||
                row.Contains("JAPAN Office") || row.Contains("USA Office") ||
                row.Contains("Settle") || row.Contains("Kazuki") ||
                row.Contains("Naoto") || row.Contains("Hinana") ||
                row.Contains("Aichi-ken"))
                continue;

            var shipMatch = Regex.Match(row, @"Shipment\s+(.+?)\s*$", RegexOptions.IgnoreCase);
            if (shipMatch.Success)
            {
                currentShipmentText = shipMatch.Groups[1].Value.Trim();
                FlushPendingLots(pendingLots, currentShipmentText);
                continue;
            }

            var lot = TryParseOfferLine(row, offerId, currentOrigin);
            if (lot != null)
            {
                lot.SourceLineNumber = i;
                lot.SourceRawLine = row;
                if (!string.IsNullOrEmpty(lot.CropYear))
                    currentCropYear = lot.CropYear;
                else if (!string.IsNullOrEmpty(currentCropYear))
                    lot.CropYear = currentCropYear;
                pendingLots.Add(lot);
            }
        }

        FlushPendingLots(pendingLots, currentShipmentText);
        result.Lots.AddRange(pendingLots);
        return result;
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
                OfferId = offerId, LotCode = lotCode, Origin = origin,
                Type = $"M/E Recap {lotCode}", SpecialSpec = "-",
                Quantity = qty, QuantityText = $"{qty} {unit}",
                OutrightPrice = outright, BasisCents = basis,
                PriceCentsPerLb = outright, SettlementMonth = settlement,
                CropYear = ParseCropYearFromContext(row)
            };
        }

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
            { qty = parsedQty; quantityDisplay = $"{qty} {unit}"; }

            var (type, specialSpec, colorSpec, leafSpec, lengthSpec, micSpec, strSpec) = ParseSpecPart(specPart);

            return new OfferLot
            {
                OfferId = offerId, LotCode = null, Origin = origin,
                CropYear = cropYear.Length == 4 ? cropYear : $"'{cropYear}",
                Type = type, SpecialSpec = specialSpec,
                Quantity = qty, QuantityText = quantityDisplay,
                OutrightPrice = outright, BasisCents = basis,
                PriceCentsPerLb = outright, SettlementMonth = settlement,
                ColorSpec = colorSpec, LeafSpec = leafSpec,
                LengthSpec = lengthSpec, MicronaireSpec = micSpec, StrengthSpec = strSpec
            };
        }

        return null;
    }

    private (string type, string? specialSpec, string? color, string? leaf, string? length, string? mic, string? strength) ParseSpecPart(string specPart)
    {
        string type = specPart;
        string? specialSpec = null;
        string? colorSpec = null, leafSpec = null, lengthSpec = null, micSpec = null, strSpec = null;

        var gptMatch = Regex.Match(specPart, @"(\d+)\s*GPT\s*min", RegexOptions.IgnoreCase);
        if (gptMatch.Success)
        { strSpec = gptMatch.Groups[1].Value; specPart = specPart.Substring(0, gptMatch.Index).Trim(); }

        var micGradeMatch = Regex.Match(specPart, @"\bG(\d)\b");
        if (micGradeMatch.Success)
        { micSpec = micGradeMatch.Value; specPart = specPart.Replace(micGradeMatch.Value, "").Trim(); }

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

        var parts = specPart.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parts.Count >= 1)
        {
            type = parts[0];
            if (parts.Count > 1) specialSpec = string.Join(" ", parts.Skip(1));
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
        var match = Regex.Match(shipmentText, @"(\d{1,2})/(\d{1,2})'?(\d{4})");
        if (match.Success)
        {
            var month1 = int.Parse(match.Groups[1].Value);
            var year = int.Parse(match.Groups[3].Value);
            try { return new DateTime(year, month1, 1, 0, 0, 0, DateTimeKind.Utc); } catch { }
        }
        var promptMatch = Regex.Match(shipmentText, @"Prompt'?(\d{4})");
        if (promptMatch.Success)
        {
            var year = int.Parse(promptMatch.Groups[1].Value);
            return new DateTime(year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }
        return null;
    }
}
