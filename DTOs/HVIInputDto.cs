namespace CBAS.Web.DTOs;

public class HVIInputDto
{
    public int HVIId { get; set; }
    public string LotCode { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public decimal? Micronaire { get; set; }
    public decimal? Length { get; set; }
    public decimal? StrengthGPT { get; set; }
    public decimal? Uniformity { get; set; }
    public decimal? ColorRd { get; set; }
    public string? ColorGrade { get; set; }
    public decimal? Leaf { get; set; }
    public string? CropYear { get; set; }
    public int? TotalBales { get; set; }
    public string? Notes { get; set; }

    // OCR fields
    public float ConfidenceScore { get; set; }
    public string? RawOcrText { get; set; }
    public OcrStatus OcrStatus { get; set; } = OcrStatus.NotProcessed;
    public string? OcrErrorMessage { get; set; }

    // Indicates if this HVI already has data filled in
    public bool HasData => Micronaire.HasValue || Length.HasValue || StrengthGPT.HasValue;
}

public enum OcrStatus
{
    NotProcessed,
    Processing,
    Success,
    Failed,
    Skipped
}
