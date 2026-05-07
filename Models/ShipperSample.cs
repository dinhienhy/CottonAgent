using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBAS.Web.Models;

public class ShipperSample
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ShipperId { get; set; }

    [ForeignKey(nameof(ShipperId))]
    public virtual Shipper Shipper { get; set; } = null!;

    public byte[] SampleOfferPdf { get; set; } = Array.Empty<byte>();

    [MaxLength(200)]
    public string? SampleOfferFileName { get; set; }

    public byte[] SampleExcelResult { get; set; } = Array.Empty<byte>();

    [MaxLength(200)]
    public string? SampleExcelFileName { get; set; }

    public string? ExtractedPdfText { get; set; }

    public string? ExtractedExcelJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
