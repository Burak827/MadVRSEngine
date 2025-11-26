namespace VendorRisk.Domain.Models;

public class RiskAssessment
{
    public int VendorId { get; set; }
    public double RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public RiskBreakdown Breakdown { get; set; } = new();
    public List<string> Reasons { get; set; } = new();
    public string Reason => string.Join(" | ", Reasons);
}
