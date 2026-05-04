using CBAS.Web.Models;

namespace CBAS.Web.Services;

public interface IPdfParserService
{
    Task<List<OfferLot>> ParseOfferPdfAsync(Stream pdfStream, int offerId);
    OfferParseResult ParseOfferPdfFull(Stream pdfStream, int offerId, string fileName = "unknown.pdf");
    Task<HVIReport?> ParseHVIPdfAsync(Stream pdfStream, string fileName);
}
