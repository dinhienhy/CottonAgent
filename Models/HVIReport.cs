using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBAS.Web.Models;

public class HVIReport
{
    [Key]
    public int HVIId { get; set; }

    [Required]
    [MaxLength(100)]
    public string LotCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Micronaire { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Length { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? StrengthGPT { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Uniformity { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? ColorRd { get; set; }

    [MaxLength(50)]
    public string? ColorGrade { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Leaf { get; set; }

    [MaxLength(50)]
    public string? CropYear { get; set; }

    public int? TotalBales { get; set; }

    public string? RawDataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<OfferLot> OfferLots { get; set; } = new List<OfferLot>();
}
