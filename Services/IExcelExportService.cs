using CBAS.Web.DTOs;

namespace CBAS.Web.Services;

public interface IExcelExportService
{
    Task<byte[]> GenerateExcelAsync(List<OutputGroupDto> groups);
}
