using CBAS.Web.DTOs;
using CBAS.Web.Models;

namespace CBAS.Web.Services;

public interface IOfferProcessingService
{
    Task<int> ProcessOfferAsync(OfferUploadDto uploadDto);
    Task<List<HVIInputDto>> GetHVIForReviewAsync(int offerId);
    Task<HVIInputDto> RunOcrForLotAsync(string lotCode, Stream pdfStream, string fileName);
    Task SaveHVIDataAsync(List<HVIInputDto> hviInputs);
    Task RecalculateOutputsAsync(int offerId);
    Task<List<OutputGroupDto>> GetProcessedOutputAsync(int offerId);
    Task<byte[]> ExportToExcelAsync(int offerId);
}
