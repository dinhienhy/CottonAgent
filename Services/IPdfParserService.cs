using CBAS.Web.Models;

namespace CBAS.Web.Services;

public interface IPdfParserService
{
    Task<List<OfferLot>> ParseOfferPdfAsync(Stream pdfStream, int offerId);
    Task<HVIReport?> ParseHVIPdfAsync(Stream pdfStream, string fileName);
}
