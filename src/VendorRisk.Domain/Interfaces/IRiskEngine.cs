using VendorRisk.Domain.Models;

namespace VendorRisk.Domain.Interfaces;

public interface IRiskEngine
{
    Task<RiskAssessment> EvaluateAsync(VendorProfile vendor, CancellationToken cancellationToken = default);
}
