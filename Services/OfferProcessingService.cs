using CBAS.Web.Data;
using CBAS.Web.DTOs;
using CBAS.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CBAS.Web.Services;

public class OfferProcessingService : IOfferProcessingService
{
    private readonly ApplicationDbContext _context;
    private readonly IPdfParserService _pdfParser;
    private readonly IExcelExportService _excelExport;
    private readonly IOcrService _ocrService;
    private const decimal LB_TO_KG = 2.20462m;

    public OfferProcessingService(
        ApplicationDbContext context,
        IPdfParserService pdfParser,
        IExcelExportService excelExport,
        IOcrService ocrService)
    {
        _context = context;
        _pdfParser = pdfParser;
        _excelExport = excelExport;
        _ocrService = ocrService;
    }

    public async Task<int> ProcessOfferAsync(OfferUploadDto uploadDto)
    {
        Console.WriteLine($"Creating offer: Supplier={uploadDto.SupplierName}, ICE={uploadDto.ICEValue}");
        
        var offer = new Offer
        {
            OfferDate = DateTime.SpecifyKind(uploadDto.OfferDate, DateTimeKind.Utc),
            SupplierName = uploadDto.SupplierName ?? "Unknown",
            FileName = uploadDto.OfferFileName ?? "Unknown",
            ICEValue = uploadDto.ICEValue,
            CommissionPercent = uploadDto.CommissionPercent,
            CreatedAt = DateTime.UtcNow
        };

        // Auto-create or find Shipper
        var shipper = await _context.Shippers
            .FirstOrDefaultAsync(s => s.Name == offer.SupplierName);
        if (shipper == null)
        {
            shipper = new Shipper { Name = offer.SupplierName, CreatedAt = DateTime.UtcNow };
            _context.Shippers.Add(shipper);
            await _context.SaveChangesAsync();
        }
        offer.ShipperId = shipper.ShipperId;

        _context.Offers.Add(offer);
        
        try
        {
            await _context.SaveChangesAsync();
            Console.WriteLine($"Offer saved. OfferId: {offer.OfferId}, ShipperId: {shipper.ShipperId}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save offer: {ex.InnerException?.Message ?? ex.Message}", ex);
        }

        // STEP 1: Parse HVI PDFs
        Console.WriteLine($"Parsing {uploadDto.HVIFiles.Count} HVI PDF files...");
        foreach (var hviFile in uploadDto.HVIFiles)
        {
            try
            {
                hviFile.FileStream.Position = 0;
                var hviReport = await _pdfParser.ParseHVIPdfAsync(hviFile.FileStream, hviFile.FileName);
                
                if (hviReport != null)
                {
                    var existingHvi = await _context.HVIReports
                        .FirstOrDefaultAsync(h => h.LotCode == hviReport.LotCode);
                    
                    if (existingHvi == null)
                    {
                        Console.WriteLine($"Adding HVI: {hviReport.LotCode} (scanned image - needs manual data)");
                        _context.HVIReports.Add(hviReport);
                    }
                    else
                    {
                        Console.WriteLine($"HVI exists: {hviReport.LotCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error HVI {hviFile.FileName}: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync();

        // STEP 2: Parse Offer PDF
        OfferParseResult? parseResult = null;
        if (uploadDto.OfferPdfStream != null)
        {
            try
            {
                uploadDto.OfferPdfStream.Position = 0;
                parseResult = _pdfParser.ParseOfferPdfFull(uploadDto.OfferPdfStream, offer.OfferId);
                Console.WriteLine($"Parsed {parseResult.Lots.Count} lots, ICE settlements: {string.Join(", ", parseResult.ICESettlements.Select(kv => $"{kv.Key}={kv.Value}"))}");

                // Store ICE settlements on the offer
                if (parseResult.ICESettlements.Count > 0)
                {
                    offer.ICESettlementsJson = JsonSerializer.Serialize(parseResult.ICESettlements);
                    // Update main ICE value from first settlement
                    var firstSettle = parseResult.ICESettlements.FirstOrDefault();
                    if (firstSettle.Value > 0)
                        offer.ICEValue = firstSettle.Value;
                }

                if (parseResult.OfferDate != default)
                    offer.OfferDate = parseResult.OfferDate;

                foreach (var lot in parseResult.Lots)
                {
                    // PriceCentsPerLb = outright = ICE + basis (already set by parser)
                    Console.WriteLine($"Lot: {lot.LotCode ?? lot.Type}, Origin={lot.Origin}, Basis={lot.BasisCents}, Outright={lot.OutrightPrice}, Ship={lot.ShipmentDateText}");
                    _context.OfferLots.Add(lot);
                }
                
                await _context.SaveChangesAsync();
                Console.WriteLine("Lots saved");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error parsing offer: {ex.Message}");
                throw new Exception($"Failed to process offer: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
        }

        // Don't generate outputs yet - wait for HVI review step
        return offer.OfferId;
    }

    public async Task<List<HVIInputDto>> GetHVIForReviewAsync(int offerId)
    {
        // Get all lots with LotCode for this offer
        var lotsWithCode = await _context.OfferLots
            .Where(l => l.OfferId == offerId && l.LotCode != null)
            .Select(l => l.LotCode!)
            .Distinct()
            .ToListAsync();

        var result = new List<HVIInputDto>();

        foreach (var lotCode in lotsWithCode)
        {
            var hvi = await _context.HVIReports.FirstOrDefaultAsync(h => h.LotCode == lotCode);

            if (hvi != null)
            {
                result.Add(new HVIInputDto
                {
                    HVIId = hvi.HVIId,
                    LotCode = hvi.LotCode,
                    FileName = hvi.FileName,
                    Micronaire = hvi.Micronaire,
                    Length = hvi.Length,
                    StrengthGPT = hvi.StrengthGPT,
                    Uniformity = hvi.Uniformity,
                    ColorRd = hvi.ColorRd,
                    ColorGrade = hvi.ColorGrade,
                    Leaf = hvi.Leaf,
                    CropYear = hvi.CropYear,
                    TotalBales = hvi.TotalBales
                });
            }
            else
            {
                // No HVI record yet - create a blank entry for manual input
                result.Add(new HVIInputDto
                {
                    HVIId = 0,
                    LotCode = lotCode,
                    FileName = ""
                });
            }
        }

        return result.OrderBy(h => h.LotCode).ToList();
    }

    public async Task<HVIInputDto> RunOcrForLotAsync(string lotCode, Stream pdfStream, string fileName)
    {
        var result = await _ocrService.ProcessHVIPdfAsync(pdfStream, fileName);

        var dto = new HVIInputDto
        {
            LotCode = lotCode,
            FileName = fileName
        };

        if (result.Success)
        {
            dto.Micronaire = result.Micronaire;
            dto.Length = result.Length;
            dto.StrengthGPT = result.StrengthGPT;
            dto.Uniformity = result.Uniformity;
            dto.ColorRd = result.ColorRd;
            dto.ColorGrade = result.ColorGrade;
            dto.Leaf = result.Leaf;
            dto.CropYear = result.CropYear;
            dto.TotalBales = result.TotalBales;
            dto.ConfidenceScore = result.Confidence;
            dto.RawOcrText = result.RawText;
            dto.OcrStatus = OcrStatus.Success;
        }
        else
        {
            dto.OcrStatus = OcrStatus.Failed;
            dto.OcrErrorMessage = result.ErrorMessage;
        }

        // Update existing HVI record if it exists
        var hvi = await _context.HVIReports.FirstOrDefaultAsync(h => h.LotCode == lotCode);
        if (hvi != null)
        {
            dto.HVIId = hvi.HVIId;
            // If record already has manually-entered data, don't overwrite
            if (hvi.Micronaire.HasValue || hvi.Length.HasValue)
            {
                dto.Micronaire = hvi.Micronaire;
                dto.Length = hvi.Length;
                dto.StrengthGPT = hvi.StrengthGPT;
                dto.Uniformity = hvi.Uniformity;
                dto.ColorRd = hvi.ColorRd;
                dto.ColorGrade = hvi.ColorGrade;
                dto.Leaf = hvi.Leaf;
                dto.CropYear = hvi.CropYear;
                dto.TotalBales = hvi.TotalBales;
            }
        }

        return dto;
    }

    public async Task SaveHVIDataAsync(List<HVIInputDto> hviInputs)
    {
        foreach (var input in hviInputs)
        {
            HVIReport? hvi;
            if (input.HVIId > 0)
            {
                hvi = await _context.HVIReports.FindAsync(input.HVIId);
                if (hvi == null) continue;
            }
            else
            {
                // Check if already exists by LotCode
                hvi = await _context.HVIReports.FirstOrDefaultAsync(h => h.LotCode == input.LotCode);
                if (hvi == null)
                {
                    hvi = new HVIReport
                    {
                        LotCode = input.LotCode,
                        FileName = input.FileName ?? $"{input.LotCode}.pdf",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.HVIReports.Add(hvi);
                }
            }

            hvi.Micronaire = input.Micronaire;
            hvi.Length = input.Length;
            hvi.StrengthGPT = input.StrengthGPT;
            hvi.Uniformity = input.Uniformity;
            hvi.ColorRd = input.ColorRd;
            hvi.ColorGrade = input.ColorGrade;
            hvi.Leaf = input.Leaf;
            hvi.CropYear = input.CropYear;
            hvi.TotalBales = input.TotalBales;
            hvi.RawDataJson = input.Notes;
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"Saved HVI data for {hviInputs.Count} lots");
    }

    public async Task RecalculateOutputsAsync(int offerId)
    {
        await SyncLotsAsync(offerId);
        await GenerateProcessedOutputsAsync(offerId);
    }

    private async Task SyncLotsAsync(int offerId)
    {
        var offer = await _context.Offers
            .Include(o => o.OfferLots)
            .FirstOrDefaultAsync(o => o.OfferId == offerId);
        if (offer == null) return;

        var shipperId = offer.ShipperId;
        if (!shipperId.HasValue) return;

        foreach (var offerLot in offer.OfferLots)
        {
            if (string.IsNullOrEmpty(offerLot.LotCode)) continue;

            var lot = await _context.Lots.FirstOrDefaultAsync(l => l.LotCode == offerLot.LotCode);

            if (lot == null)
            {
                // Create new Lot
                lot = new Lot
                {
                    LotCode = offerLot.LotCode,
                    ShipperId = shipperId.Value,
                    Origin = offerLot.Origin,
                    CropYear = offerLot.CropYear,
                    Type = offerLot.Type,
                    QuantityOriginal = offerLot.Quantity,
                    QuantityAvailable = offerLot.Quantity,
                    Status = LotStatus.Available,
                    LatestOfferId = offerId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Lots.Add(lot);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Update existing Lot — quantity from latest offer replaces old
                lot.QuantityAvailable = offerLot.Quantity;
                lot.LatestOfferId = offerId;
                lot.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(offerLot.Origin)) lot.Origin = offerLot.Origin;
                if (!string.IsNullOrEmpty(offerLot.CropYear)) lot.CropYear = offerLot.CropYear;
            }

            // Link OfferLot → Lot
            offerLot.MasterLotId = lot.Id;

            // Link HVIReport → Lot if exists
            var hvi = await _context.HVIReports.FirstOrDefaultAsync(h => h.LotCode == offerLot.LotCode);
            if (hvi != null)
            {
                hvi.MasterLotId = lot.Id;
                lot.HVIReportId = hvi.HVIId;
            }
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"Synced lots for offer {offerId}");
    }

    public async Task<List<OutputGroupDto>> GetProcessedOutputAsync(int offerId)
    {
        var outputs = await _context.ProcessedOutputs
            .Where(po => po.OfferId == offerId)
            .OrderBy(po => po.ShipmentDate)
            .ThenBy(po => po.STT)
            .ToListAsync();

        var groups = outputs
            .GroupBy(o => o.ShipmentDateText ?? (o.ShipmentDate.HasValue ? $"{o.ShipmentDate.Value:M/yyyy} SO" : "No Date"))
            .Select(g => new OutputGroupDto
            {
                ShipmentDate = g.First().ShipmentDate,
                GroupTitle = g.Key,
                Rows = g.Select(o => new OutputRowDto
                {
                    STT = o.STT,
                    Origin = o.Origin,
                    CropYear = o.CropYear,
                    Quantity = o.Quantity,
                    QuantityText = o.QuantityText,
                    Type = o.Type,
                    SpecialSpec = o.SpecialSpec,
                    Color = o.Color,
                    Leaf = o.Leaf,
                    Length = o.Length,
                    Micronaire = o.Micronaire,
                    MicronaireText = o.MicronaireText,
                    StrengthMin = o.StrengthMin,
                    StrengthText = o.StrengthText,
                    Basis = o.Basis,
                    ShipmentDate = o.ShipmentDate,
                    ShipmentDateText = o.ShipmentDateText,
                    PriceCentsPerKg = o.PriceCentsPerKg,
                    PriceWithCommission = o.PriceWithCommission,
                    NetPrice = o.NetPrice,
                    Notes = o.Notes
                }).ToList()
            })
            .ToList();

        return groups;
    }

    public async Task<byte[]> ExportToExcelAsync(int offerId)
    {
        var groups = await GetProcessedOutputAsync(offerId);
        return await _excelExport.GenerateExcelAsync(groups);
    }

    private async Task GenerateProcessedOutputsAsync(int offerId)
    {
        var offer = await _context.Offers
            .Include(o => o.OfferLots)
            .FirstOrDefaultAsync(o => o.OfferId == offerId);

        if (offer == null) return;

        var existingOutputs = await _context.ProcessedOutputs
            .Where(po => po.OfferId == offerId)
            .ToListAsync();
        
        _context.ProcessedOutputs.RemoveRange(existingOutputs);
        await _context.SaveChangesAsync();

        // Parse ICE settlements from offer
        Dictionary<string, decimal>? iceSettlements = null;
        if (!string.IsNullOrEmpty(offer.ICESettlementsJson))
        {
            iceSettlements = JsonSerializer.Deserialize<Dictionary<string, decimal>>(offer.ICESettlementsJson);
        }

        var hviReports = await _context.HVIReports.ToListAsync();
        var hviDict = hviReports.ToDictionary(h => h.LotCode, h => h);

        var sortedLots = offer.OfferLots
            .OrderBy(l => l.ShipmentDate)
            .ThenBy(l => l.LotCode ?? l.Type)
            .ToList();

        int stt = 1;
        foreach (var lot in sortedLots)
        {
            // Get ICE value for this lot's settlement month
            var iceValue = offer.ICEValue;
            if (iceSettlements != null && !string.IsNullOrEmpty(lot.SettlementMonth))
            {
                if (iceSettlements.TryGetValue(lot.SettlementMonth, out var settleIce))
                    iceValue = settleIce;
            }

            // Look up HVI data by lot code
            HVIReport? hvi = null;
            if (!string.IsNullOrEmpty(lot.LotCode))
                hviDict.TryGetValue(lot.LotCode, out hvi);

            // Price calculation: outright (c/lb) is already ICE + basis
            var outrightCentsPerLb = lot.OutrightPrice > 0 ? lot.OutrightPrice : (iceValue + lot.BasisCents);
            var priceCentsPerKg = Math.Round(outrightCentsPerLb * LB_TO_KG, 2);
            var commission = offer.CommissionPercent;
            var priceWithCommission = Math.Round(priceCentsPerKg - commission, 2);
            var netPrice = priceWithCommission;

            // Basis in points for display (cents × 100)
            var basisPoints = lot.BasisCents * 100m;

            // Determine color, leaf, length, mic, strength from HVI or offer spec
            string? colorDisplay = null;
            decimal? leafDisplay = null;
            decimal? lengthDisplay = null;
            decimal? micDisplay = null;
            string? micText = null;
            decimal? strDisplay = null;
            string? strText = null;

            if (hvi != null)
            {
                // Use HVI data
                colorDisplay = hvi.ColorRd?.ToString("F0") ?? hvi.ColorGrade;
                leafDisplay = hvi.Leaf;
                lengthDisplay = hvi.Length;
                micDisplay = hvi.Micronaire;
                strDisplay = hvi.StrengthGPT;
            }
            else
            {
                // Use offer spec data (for generic lines)
                colorDisplay = lot.ColorSpec;
                if (decimal.TryParse(lot.LeafSpec, out var ls)) leafDisplay = ls;
                if (decimal.TryParse(lot.LengthSpec, out var le)) lengthDisplay = le;
                micText = lot.MicronaireSpec;
                if (decimal.TryParse(lot.StrengthSpec, out var st)) strDisplay = st;
                else strText = lot.StrengthSpec;
            }

            var output = new ProcessedOutput
            {
                OfferId = offerId,
                LotId = lot.LotId,
                STT = stt++,
                Origin = lot.Origin,
                CropYear = lot.CropYear,
                Quantity = lot.Quantity,
                QuantityText = lot.QuantityText,
                Type = lot.Type,
                SpecialSpec = lot.SpecialSpec,
                Color = colorDisplay,
                Leaf = leafDisplay,
                Length = lengthDisplay,
                Micronaire = micDisplay,
                MicronaireText = micText,
                StrengthMin = strDisplay,
                StrengthText = strText,
                Basis = basisPoints,
                ShipmentDate = lot.ShipmentDate,
                ShipmentDateText = lot.ShipmentDateText,
                PriceCentsPerKg = priceCentsPerKg,
                PriceWithCommission = priceWithCommission,
                NetPrice = netPrice,
                Notes = hvi != null ? "Full HVI chi tiết" : (lot.LotCode != null ? "HVI chưa nhập" : "")
            };

            _context.ProcessedOutputs.Add(output);
        }

        await _context.SaveChangesAsync();
    }
}
