using System.ComponentModel.DataAnnotations;

namespace CBAS.Web.Models;

public class Shipper
{
    [Key]
    public int ShipperId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ContactInfo { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();
    public virtual ICollection<Lot> Lots { get; set; } = new List<Lot>();
}
