using CBAS.Web.DTOs;
using CBAS.Web.Models;

namespace CBAS.Web.Services;

public interface IOfferProcessingService
{
    Task<int> ProcessOfferAsync(OfferUploadDto uploadDto);
    Task<List<OutputGroupDto>> GetProcessedOutputAsync(int offerId);
    Task<byte[]> ExportToExcelAsync(int offerId);
}
