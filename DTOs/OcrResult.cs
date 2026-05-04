namespace CBAS.Web.DTOs;

public class OcrResult
{
    public bool Success { get; set; }
    public float Confidence { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    // Parsed HVI fields
    public decimal? Micronaire { get; set; }
    public decimal? Length { get; set; }
    public decimal? StrengthGPT { get; set; }
    public decimal? Uniformity { get; set; }
    public decimal? ColorRd { get; set; }
    public decimal? PlusB { get; set; }
    public string? ColorGrade { get; set; }
    public decimal? Leaf { get; set; }
    public string? CropYear { get; set; }
    public int? TotalBales { get; set; }
}
