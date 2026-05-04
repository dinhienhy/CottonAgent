using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBAS.Web.Models;

public class OfferLot
{
    [Key]
    public int LotId { get; set; }

    [Required]
    public int OfferId { get; set; }

    [ForeignKey(nameof(OfferId))]
    public virtual Offer Offer { get; set; } = null!;

    [MaxLength(100)]
    public string? LotCode { get; set; }

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

    [Column(TypeName = "decimal(10,2)")]
    public decimal BasisCents { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal OutrightPrice { get; set; }

    [MaxLength(20)]
    public string? SettlementMonth { get; set; }

    [MaxLength(100)]
    public string? ShipmentDateText { get; set; }

    public DateTime? ShipmentDate { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal PriceCentsPerLb { get; set; }

    [MaxLength(50)]
    public string? ColorSpec { get; set; }

    [MaxLength(50)]
    public string? LeafSpec { get; set; }

    [MaxLength(50)]
    public string? LengthSpec { get; set; }

    [MaxLength(50)]
    public string? MicronaireSpec { get; set; }

    [MaxLength(50)]
    public string? StrengthSpec { get; set; }

    public int? MasterLotId { get; set; }

    [ForeignKey(nameof(MasterLotId))]
    public virtual Lot? MasterLot { get; set; }
}
