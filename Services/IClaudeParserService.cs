using CBAS.Web.DTOs;

namespace CBAS.Web.Services;

public interface IClaudeParserService
{
    bool IsAvailable { get; }
    Task<ClaudeOfferResponse?> ParseOfferTextAsync(string pdfText, int? shipperId);
}
