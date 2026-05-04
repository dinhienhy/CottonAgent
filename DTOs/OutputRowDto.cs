namespace CBAS.Web.DTOs;

public class OutputRowDto
{
    public int STT { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string CropYear { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? QuantityText { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? SpecialSpec { get; set; }
    public string? Color { get; set; }
    public decimal? Leaf { get; set; }
    public decimal? Length { get; set; }
    public decimal? Micronaire { get; set; }
    public string? MicronaireText { get; set; }
    public decimal? StrengthMin { get; set; }
    public string? StrengthText { get; set; }
    public decimal Basis { get; set; }
    public DateTime? ShipmentDate { get; set; }
    public string? ShipmentDateText { get; set; }
    public decimal PriceCentsPerKg { get; set; }
    public decimal PriceWithCommission { get; set; }
    public decimal NetPrice { get; set; }
    public string? Notes { get; set; }
}

public class OutputGroupDto
{
    public DateTime? ShipmentDate { get; set; }
    public string GroupTitle { get; set; } = string.Empty;
    public List<OutputRowDto> Rows { get; set; } = new();
}
