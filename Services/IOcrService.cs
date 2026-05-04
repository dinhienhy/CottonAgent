using CBAS.Web.DTOs;

namespace CBAS.Web.Services;

public interface IOcrService
{
    bool IsAvailable { get; }
    Task<OcrResult> ProcessHVIPdfAsync(Stream pdfStream, string fileName);
}
