using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBAS.Web.Models;

public class Offer
{
    [Key]
    public int OfferId { get; set; }

    [Required]
    public DateTime OfferDate { get; set; }

    [Required]
    [MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal ICEValue { get; set; } = 84.19m;

    [Column(TypeName = "decimal(5,2)")]
    public decimal CommissionPercent { get; set; } = 2.00m;

    public string? ICESettlementsJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<OfferLot> OfferLots { get; set; } = new List<OfferLot>();
}
