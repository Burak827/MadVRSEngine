using VendorRisk.Domain.Interfaces;
using VendorRisk.Domain.Models;
using Microsoft.Extensions.Logging;

namespace VendorRisk.Infrastructure.Services;

public class RuleEngineService : IRiskEngine
{
    private readonly IRiskFactorMatrixProvider _matrixProvider;
    private readonly ILogger<RuleEngineService> _logger;
    private readonly IRiskRulesProvider _rulesProvider;

    public RuleEngineService(IRiskFactorMatrixProvider matrixProvider, IRiskRulesProvider rulesProvider, ILogger<RuleEngineService> logger)
    {
        _matrixProvider = matrixProvider;
        _rulesProvider = rulesProvider;
        _logger = logger;
    }

    public async Task<RiskAssessment> EvaluateAsync(VendorProfile vendor, CancellationToken cancellationToken = default)
    {
        // Defensive init to avoid null refs when requests omit collections/owned types.
        vendor.Documents ??= new VendorDocuments();
        vendor.SecurityCerts ??= new List<string>();
        var reasons = new List<string>();
        var matrix = await _matrixProvider.GetMatrixAsync(cancellationToken); // Similarity data for explainability
        var rules = await _rulesProvider.GetAsync(cancellationToken); // Tunable thresholds/weights

        var financial = ComputeFinancialRisk(vendor, rules, reasons);
        var operational = ComputeOperationalRisk(vendor, matrix, rules, reasons);
        var security = ComputeSecurityComplianceRisk(vendor, matrix, rules, reasons);

        var score = (financial * rules.Weights.Financial) +
                    (operational * rules.Weights.Operational) +
                    (security * rules.Weights.Security);
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

    private static double ComputeFinancialRisk(VendorProfile vendor, RiskRulesConfig rules, ICollection<string> reasons)
    {
        if (vendor.FinancialHealth < rules.Financial.LowThreshold)
        {
            reasons.Add("Financial health below 50 triggers high financial risk.");
            return rules.Financial.HighRisk;
        }

        if (vendor.FinancialHealth > rules.Financial.HighThreshold)
        {
            reasons.Add("Financial health above 80 reduces financial risk.");
            return rules.Financial.LowRisk;
        }

        if (vendor.FinancialHealth >= rules.Financial.LowThreshold && vendor.FinancialHealth <= rules.Financial.MidMaxThreshold)
        {
            reasons.Add("Financial health between 50-65 indicates moderate debt/liquidity risk.");
            return rules.Financial.MidRisk;
        }

        reasons.Add("Financial health in a stable range.");
        return rules.Financial.StableRisk;
    }

    private static double ComputeOperationalRisk(VendorProfile vendor, RiskFactorMatrix matrix, RiskRulesConfig rules, ICollection<string> reasons)
    {
        // Base operational exposure before evaluating SLA/incidents.
        double risk = rules.Operational.Base;

        if (vendor.SlaUptime < rules.Operational.SlaLowThreshold)
        {
            risk += rules.Operational.SlaPenalty; // SLA under threshold
            risk += GetSimilarityImpact("slaDrop", matrix.OperationalRisk, rules.SimilarityScale.Operational);
            reasons.Add("SLA uptime below 95% increases operational exposure.");
            AppendSimilarRisks("slaDrop", matrix.OperationalRisk, reasons);
        }
        else if (vendor.SlaUptime > rules.Operational.SlaHighThreshold)
        {
            risk -= rules.Operational.SlaBonus;
            reasons.Add("SLA uptime above 99% reduces operational risk.");
        }

        if (vendor.MajorIncidents > 0)
        {
            var incidentRisk = Math.Min(rules.Operational.IncidentMax, vendor.MajorIncidents * rules.Operational.IncidentStep);
            risk += incidentRisk;
            risk += GetSimilarityImpact("majorIncident", matrix.OperationalRisk, rules.SimilarityScale.Operational);
            reasons.Add($"Recorded {vendor.MajorIncidents} major incidents in the last 12 months.");
            AppendSimilarRisks("majorIncident", matrix.OperationalRisk, reasons);
        }

        return Clamp(risk);
    }

    private static double ComputeSecurityComplianceRisk(VendorProfile vendor, RiskFactorMatrix matrix, RiskRulesConfig rules, ICollection<string> reasons)
    {
        // Start from baseline then add penalties for missing certs/docs.
        double risk = rules.Security.Base;
        var hasIso = vendor.SecurityCerts.Any(c => c.Equals("ISO27001", StringComparison.OrdinalIgnoreCase));

        if (!hasIso)
        {
            risk += rules.Security.MissingIsoPenalty; // Missing ISO baseline
            risk += GetSimilarityImpact("missingISO27001", matrix.SecurityRisk, rules.SimilarityScale.Security);
            reasons.Add("Missing ISO27001 certification elevates security risk.");
            AppendSimilarRisks("missingISO27001", matrix.SecurityRisk, reasons);
        }

        if (!vendor.Documents.PrivacyPolicyValid)
        {
            risk += rules.Security.PrivacyPenalty; // Compliance document stale
            risk += GetSimilarityImpact("expiredPrivacyPolicy", matrix.ComplianceRisk, rules.SimilarityScale.Compliance);
            reasons.Add("Privacy policy expired or missing.");
            AppendSimilarRisks("expiredPrivacyPolicy", matrix.ComplianceRisk, reasons);
        }

        if (!vendor.Documents.PentestReportValid)
        {
            risk += rules.Security.PentestPenalty; // Pentest missing/failed
            risk += GetSimilarityImpact("failedPenTest", matrix.SecurityRisk, rules.SimilarityScale.Security);
            reasons.Add("Failed or missing penetration test report.");
            AppendSimilarRisks("failedPenTest", matrix.SecurityRisk, reasons);
        }

        if (!vendor.SecurityCerts.Any())
        {
            risk += rules.Security.NoCertPenalty;
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

    private static double GetSimilarityImpact(string key, Dictionary<string, Dictionary<string, double>> table, double scale)
    {
        if (!table.TryGetValue(key, out var related) || related.Count == 0)
        {
            return 0;
        }

        var max = related.Max(kvp => kvp.Value);
        return max * scale;
    }

    private static double Clamp(double value) => Math.Max(0, Math.Min(1, value));
}
