using CBAS.Web.Data;
using CBAS.Web.DTOs;
using CBAS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CBAS.Web.Services;

public class OfferProcessingService : IOfferProcessingService
{
    private readonly ApplicationDbContext _context;
    private readonly IPdfParserService _pdfParser;
    private readonly IExcelExportService _excelExport;

    public OfferProcessingService(
        ApplicationDbContext context,
        IPdfParserService pdfParser,
        IExcelExportService excelExport)
    {
        _context = context;
        _pdfParser = pdfParser;
        _excelExport = excelExport;
    }

    public async Task<int> ProcessOfferAsync(OfferUploadDto uploadDto)
    {
        Console.WriteLine($"Creating offer: Supplier={uploadDto.SupplierName}, ICE={uploadDto.ICEValue}, Date={uploadDto.OfferDate}");
        
        var offer = new Offer
        {
            OfferDate = uploadDto.OfferDate,
            SupplierName = uploadDto.SupplierName ?? "Unknown",
            FileName = uploadDto.OfferFileName ?? "Unknown",
            ICEValue = uploadDto.ICEValue,
            CommissionPercent = uploadDto.CommissionPercent,
            CreatedAt = DateTime.UtcNow
        };

        _context.Offers.Add(offer);
        
        try
        {
            Console.WriteLine("Saving offer to database...");
            await _context.SaveChangesAsync();
            Console.WriteLine($"Offer saved successfully. OfferId: {offer.OfferId}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error saving offer: {ex.Message}");
            Console.Error.WriteLine($"Inner exception: {ex.InnerException?.Message}");
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            throw new Exception($"Failed to save offer: {ex.InnerException?.Message ?? ex.Message}", ex);
        }

        if (uploadDto.OfferPdfStream != null)
        {
            uploadDto.OfferPdfStream.Position = 0;
            var lots = await _pdfParser.ParseOfferPdfAsync(uploadDto.OfferPdfStream, offer.OfferId);
            
            foreach (var lot in lots)
            {
                lot.PriceCentsPerLb = CalculatePricePerLb(offer.ICEValue, lot.BasisPoints);
                _context.OfferLots.Add(lot);
            }
            
            await _context.SaveChangesAsync();
        }

        foreach (var hviFile in uploadDto.HVIFiles)
        {
            hviFile.FileStream.Position = 0;
            var hviReport = await _pdfParser.ParseHVIPdfAsync(hviFile.FileStream, hviFile.FileName);
            
            if (hviReport != null)
            {
                var existingHvi = await _context.HVIReports
                    .FirstOrDefaultAsync(h => h.LotCode == hviReport.LotCode);
                
                if (existingHvi == null)
                {
                    _context.HVIReports.Add(hviReport);
                }
            }
        }

        await _context.SaveChangesAsync();

        await GenerateProcessedOutputsAsync(offer.OfferId);

        return offer.OfferId;
    }

    public async Task<List<OutputGroupDto>> GetProcessedOutputAsync(int offerId)
    {
        var outputs = await _context.ProcessedOutputs
            .Where(po => po.OfferId == offerId)
            .OrderBy(po => po.ShipmentDate)
            .ThenBy(po => po.STT)
            .ToListAsync();

        var groups = outputs
            .GroupBy(o => o.ShipmentDate)
            .Select(g => new OutputGroupDto
            {
                ShipmentDate = g.Key,
                GroupTitle = g.Key.HasValue 
                    ? $"{g.Key.Value:dd/MM/yyyy} SO" 
                    : "No Shipment Date",
                Rows = g.Select(o => new OutputRowDto
                {
                    STT = o.STT,
                    Origin = o.Origin,
                    CropYear = o.CropYear,
                    Quantity = o.Quantity,
                    Type = o.Type,
                    SpecialSpec = o.SpecialSpec,
                    Color = o.Color,
                    Leaf = o.Leaf,
                    Length = o.Length,
                    Micronaire = o.Micronaire,
                    StrengthMin = o.StrengthMin,
                    Basis = o.Basis,
                    ShipmentDate = o.ShipmentDate,
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

        var hviReports = await _context.HVIReports.ToListAsync();
        var hviDict = hviReports.ToDictionary(h => h.LotCode, h => h);

        var sortedLots = offer.OfferLots
            .OrderBy(l => l.ShipmentDate)
            .ThenBy(l => l.LotCode)
            .ToList();

        int stt = 1;
        foreach (var lot in sortedLots)
        {
            hviDict.TryGetValue(lot.LotCode, out var hvi);

            var priceCentsPerKg = CalculatePricePerKg(offer.ICEValue, lot.BasisPoints);
            var commission = offer.CommissionPercent;
            var priceWithCommission = priceCentsPerKg - commission;
            var netPrice = priceWithCommission;

            var output = new ProcessedOutput
            {
                OfferId = offerId,
                LotId = lot.LotId,
                STT = stt++,
                Origin = lot.Origin,
                CropYear = lot.CropYear,
                Quantity = lot.Quantity,
                Type = lot.Type,
                SpecialSpec = lot.SpecialSpec,
                Color = hvi?.ColorGrade ?? (hvi?.ColorRd.HasValue == true ? $"Rd {hvi.ColorRd:F2}" : null),
                Leaf = hvi?.Leaf,
                Length = hvi?.Length,
                Micronaire = hvi?.Micronaire,
                StrengthMin = hvi?.StrengthGPT,
                Basis = lot.BasisPoints,
                ShipmentDate = lot.ShipmentDate,
                PriceCentsPerKg = priceCentsPerKg,
                PriceWithCommission = priceWithCommission,
                NetPrice = netPrice,
                Notes = $"Toyoshima"
            };

            _context.ProcessedOutputs.Add(output);
        }

        await _context.SaveChangesAsync();
    }

    private decimal CalculatePricePerLb(decimal iceValue, decimal basisPoints)
    {
        return iceValue + (basisPoints / 100m);
    }

    private decimal CalculatePricePerKg(decimal iceValue, decimal basisPoints)
    {
        var pricePerLb = CalculatePricePerLb(iceValue, basisPoints);
        return pricePerLb * 2.20462m;
    }
}
