using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VendorRisk.Domain.Interfaces;
using VendorRisk.Domain.Models;
using VendorRisk.Infrastructure.Services;

namespace VendorRisk.Tests;

public class RuleEngineServiceTests
{
    private static RiskFactorMatrix CreateMatrix() =>
        new()
        {
            OperationalRisk = new Dictionary<string, Dictionary<string, double>>
            {
                ["slaDrop"] = new() { ["downtime"] = 0.87, ["slowTicketResolution"] = 0.83 },
                ["majorIncident"] = new() { ["recurringIncidents"] = 0.88 }
            },
            SecurityRisk = new Dictionary<string, Dictionary<string, double>>
            {
                ["missingISO27001"] = new() { ["weakAccessControl"] = 0.84 },
                ["failedPenTest"] = new() { ["internalVulnerabilities"] = 0.88 }
            },
            ComplianceRisk = new Dictionary<string, Dictionary<string, double>>
            {
                ["expiredPrivacyPolicy"] = new() { ["missingNDA"] = 0.81 }
            }
        };

    [Fact]
    public async Task EvaluateAsync_ReturnsCritical_WhenCertificationsAndDocsMissing()
    {
        var provider = new Mock<IRiskFactorMatrixProvider>();
        provider.Setup(p => p.GetMatrixAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMatrix());

        var engine = new RuleEngineService(provider.Object, NullLogger<RuleEngineService>.Instance);

        var vendor = new VendorProfile
        {
            Id = 1,
            Name = "High Risk Vendor",
            FinancialHealth = 48,
            SlaUptime = 88,
            MajorIncidents = 2,
            SecurityCerts = new List<string>(),
            Documents = new VendorDocuments { ContractValid = false, PrivacyPolicyValid = false, PentestReportValid = false }
        };

        var assessment = await engine.EvaluateAsync(vendor);

        assessment.RiskLevel.Should().Be(RiskLevel.Critical);
        assessment.RiskScore.Should().BeGreaterThan(0.8);
        assessment.Reasons.Should().Contain(reason => reason.Contains("ISO27001"));
        assessment.Reasons.Should().Contain(reason => reason.Contains("Privacy policy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsLowRisk_ForHealthyProfile()
    {
        var provider = new Mock<IRiskFactorMatrixProvider>();
        provider.Setup(p => p.GetMatrixAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMatrix());

        var engine = new RuleEngineService(provider.Object, NullLogger<RuleEngineService>.Instance);

        var vendor = new VendorProfile
        {
            Id = 2,
            Name = "Low Risk Vendor",
            FinancialHealth = 92,
            SlaUptime = 99,
            MajorIncidents = 0,
            SecurityCerts = new List<string> { "ISO27001" },
            Documents = new VendorDocuments { ContractValid = true, PrivacyPolicyValid = true, PentestReportValid = true }
        };

        var assessment = await engine.EvaluateAsync(vendor);

        assessment.RiskLevel.Should().Be(RiskLevel.Low);
        assessment.RiskScore.Should().BeLessThan(0.4);
        assessment.Reasons.Should().Contain(reason => reason.Contains("Financial health", StringComparison.OrdinalIgnoreCase));
    }
}
