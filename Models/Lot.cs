using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBAS.Web.Models;

public class Lot
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string LotCode { get; set; } = string.Empty;

    public int ShipperId { get; set; }

    [ForeignKey(nameof(ShipperId))]
    public virtual Shipper Shipper { get; set; } = null!;

    [MaxLength(100)]
    public string Origin { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CropYear { get; set; }

    [MaxLength(100)]
    public string? Type { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal QuantityOriginal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal QuantityAvailable { get; set; }

    public LotStatus Status { get; set; } = LotStatus.Available;

    public int? LatestOfferId { get; set; }

    [ForeignKey(nameof(LatestOfferId))]
    public virtual Offer? LatestOffer { get; set; }

    public int? HVIReportId { get; set; }

    [ForeignKey(nameof(HVIReportId))]
    public virtual HVIReport? HVIReport { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum LotStatus
{
    Available,
    Reserved,
    Sold
}
