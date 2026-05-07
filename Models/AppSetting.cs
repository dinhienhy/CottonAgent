using System.ComponentModel.DataAnnotations;

namespace CBAS.Web.Models;

public class AppSetting
{
    [Key]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
