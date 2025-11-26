using VendorRisk.Domain.Interfaces;
using VendorRisk.Domain.Models;
using Microsoft.Extensions.Logging;

namespace VendorRisk.Infrastructure.Services;

public class RuleEngineService : IRiskEngine
{
    private readonly IRiskFactorMatrixProvider _matrixProvider;
    private readonly ILogger<RuleEngineService> _logger;

    public RuleEngineService(IRiskFactorMatrixProvider matrixProvider, ILogger<RuleEngineService> logger)
    {
        _matrixProvider = matrixProvider;
        _logger = logger;
    }

    public async Task<RiskAssessment> EvaluateAsync(VendorProfile vendor, CancellationToken cancellationToken = default)
    {
        vendor.Documents ??= new VendorDocuments();
        vendor.SecurityCerts ??= new List<string>();
        var reasons = new List<string>();
        var matrix = await _matrixProvider.GetMatrixAsync(cancellationToken);

        var financial = ComputeFinancialRisk(vendor, reasons);
        var operational = ComputeOperationalRisk(vendor, matrix, reasons);
        var security = ComputeSecurityComplianceRisk(vendor, matrix, reasons);

        var score = (financial * 0.4) + (operational * 0.3) + (security * 0.3);
        var level = DetermineRiskLevel(score);

        _logger.LogInformation("Computed risk for {Vendor}: score {Score} ({Level})", vendor.Name, score, level);

        return new RiskAssessment
        {
            VendorId = vendor.Id,
            RiskScore = Math.Round(score, 2),
            RiskLevel = level,
            Breakdown = new RiskBreakdown
            {
                FinancialRisk = Math.Round(financial, 2),
                OperationalRisk = Math.Round(operational, 2),
                SecurityComplianceRisk = Math.Round(security, 2)
            },
            Reasons = reasons
        };
    }

    private static double ComputeFinancialRisk(VendorProfile vendor, ICollection<string> reasons)
    {
        if (vendor.FinancialHealth < 50)
        {
            reasons.Add("Financial health below 50 triggers high financial risk.");
            return 0.85;
        }

        if (vendor.FinancialHealth > 80)
        {
            reasons.Add("Financial health above 80 reduces financial risk.");
            return 0.25;
        }

        if (vendor.FinancialHealth is >= 50 and <= 65)
        {
            reasons.Add("Financial health between 50-65 indicates moderate debt/liquidity risk.");
            return 0.6;
        }

        reasons.Add("Financial health in a stable range.");
        return 0.45;
    }

    private static double ComputeOperationalRisk(VendorProfile vendor, RiskFactorMatrix matrix, ICollection<string> reasons)
    {
        double risk = 0.4;

        if (vendor.SlaUptime < 95)
        {
            risk += 0.25;
            reasons.Add("SLA uptime below 95% increases operational exposure.");
            AppendSimilarRisks("slaDrop", matrix.OperationalRisk, reasons);
        }
        else if (vendor.SlaUptime > 99)
        {
            risk -= 0.1;
            reasons.Add("SLA uptime above 99% reduces operational risk.");
        }

        if (vendor.MajorIncidents > 0)
        {
            var incidentRisk = Math.Min(0.3, vendor.MajorIncidents * 0.07);
            risk += incidentRisk;
            reasons.Add($"Recorded {vendor.MajorIncidents} major incidents in the last 12 months.");
            AppendSimilarRisks("majorIncident", matrix.OperationalRisk, reasons);
        }

        return Clamp(risk);
    }

    private static double ComputeSecurityComplianceRisk(VendorProfile vendor, RiskFactorMatrix matrix, ICollection<string> reasons)
    {
        double risk = 0.35;
        var hasIso = vendor.SecurityCerts.Any(c => c.Equals("ISO27001", StringComparison.OrdinalIgnoreCase));

        if (!hasIso)
        {
            risk += 0.25;
            reasons.Add("Missing ISO27001 certification elevates security risk.");
            AppendSimilarRisks("missingISO27001", matrix.SecurityRisk, reasons);
        }

        if (!vendor.Documents.PrivacyPolicyValid)
        {
            risk += 0.2;
            reasons.Add("Privacy policy expired or missing.");
            AppendSimilarRisks("expiredPrivacyPolicy", matrix.ComplianceRisk, reasons);
        }

        if (!vendor.Documents.PentestReportValid)
        {
            risk += 0.25;
            reasons.Add("Failed or missing penetration test report.");
            AppendSimilarRisks("failedPenTest", matrix.SecurityRisk, reasons);
        }

        if (!vendor.SecurityCerts.Any())
        {
            risk += 0.1;
            reasons.Add("No security certifications provided.");
        }

        return Clamp(risk);
    }

    private static RiskLevel DetermineRiskLevel(double score) =>
        score switch
        {
            < 0.35 => RiskLevel.Low,
            < 0.6 => RiskLevel.Medium,
            < 0.8 => RiskLevel.High,
            _ => RiskLevel.Critical
        };

    private static void AppendSimilarRisks(string key, Dictionary<string, Dictionary<string, double>> table, ICollection<string> reasons)
    {
        if (!table.TryGetValue(key, out var related) || related.Count == 0)
        {
            return;
        }

        var topRelated = related
            .OrderByDescending(kvp => kvp.Value)
            .Take(2)
            .Select(kvp => $"{kvp.Key} ({kvp.Value:0.00})");

        reasons.Add($"Related risk patterns: {string.Join(", ", topRelated)}");
    }

    private static double Clamp(double value) => Math.Max(0, Math.Min(1, value));
}
