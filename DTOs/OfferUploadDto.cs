namespace CBAS.Web.DTOs;

public class OfferUploadDto
{
    public string SupplierName { get; set; } = string.Empty;
    public DateTime OfferDate { get; set; } = DateTime.Today;
    public decimal ICEValue { get; set; } = 84.19m;
    public decimal CommissionPercent { get; set; } = 2.00m;
    public Stream? OfferPdfStream { get; set; }
    public string? OfferFileName { get; set; }
    public List<HVIFileDto> HVIFiles { get; set; } = new();
}

public class HVIFileDto
{
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
}
