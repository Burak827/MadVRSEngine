namespace VendorRisk.Domain.Models;

public class RiskFactorMatrix
{
    public Dictionary<string, Dictionary<string, double>> FinancialRisk { get; set; } = new();
    public Dictionary<string, Dictionary<string, double>> OperationalRisk { get; set; } = new();
    public Dictionary<string, Dictionary<string, double>> SecurityRisk { get; set; } = new();
    public Dictionary<string, Dictionary<string, double>> ComplianceRisk { get; set; } = new();
}
