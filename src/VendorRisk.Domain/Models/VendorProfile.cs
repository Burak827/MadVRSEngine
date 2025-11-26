using System.ComponentModel.DataAnnotations;

namespace VendorRisk.Domain.Models;

public class VendorProfile
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 0-100 financial health score; lower numbers mean weaker financial stability.
    /// </summary>
    [Range(0, 100)]
    public int FinancialHealth { get; set; }

    /// <summary>
    /// Target SLA uptime percentage.
    /// </summary>
    [Range(0, 100)]
    public int SlaUptime { get; set; }

    /// <summary>
    /// Count of P1/P0 incidents in last 12 months.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MajorIncidents { get; set; }

    public List<string> SecurityCerts { get; set; } = new();

    public VendorDocuments Documents { get; set; } = new();
}
