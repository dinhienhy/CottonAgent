using CBAS.Web.Models;

namespace CBAS.Web.Services;

public interface IPdfParserService
{
    Task<List<OfferLot>> ParseOfferPdfAsync(Stream pdfStream, int offerId);
    OfferParseResult ParseOfferPdfFull(Stream pdfStream, int offerId, string fileName = "unknown.pdf");
    Task<OfferParseResult> ParseOfferPdfWithAIAsync(Stream pdfStream, int offerId, string fileName, int? shipperId);
    Task<HVIReport?> ParseHVIPdfAsync(Stream pdfStream, string fileName);
}
