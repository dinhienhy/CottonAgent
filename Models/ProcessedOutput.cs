using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBAS.Web.Models;

public class ProcessedOutput
{
    [Key]
    public int OutputId { get; set; }

    [Required]
    public int OfferId { get; set; }

    [ForeignKey(nameof(OfferId))]
    public virtual Offer Offer { get; set; } = null!;

    [Required]
    public int LotId { get; set; }

    [ForeignKey(nameof(LotId))]
    public virtual OfferLot OfferLot { get; set; } = null!;

    public int STT { get; set; }

    [MaxLength(100)]
    public string Origin { get; set; } = string.Empty;

    [MaxLength(50)]
    public string CropYear { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Quantity { get; set; }

    [MaxLength(100)]
    public string? QuantityText { get; set; }

    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? SpecialSpec { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Leaf { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Length { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Micronaire { get; set; }

    [MaxLength(50)]
    public string? MicronaireText { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? StrengthMin { get; set; }

    [MaxLength(50)]
    public string? StrengthText { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Basis { get; set; }

    public DateTime? ShipmentDate { get; set; }

    [MaxLength(100)]
    public string? ShipmentDateText { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal PriceCentsPerKg { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal PriceWithCommission { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal NetPrice { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
