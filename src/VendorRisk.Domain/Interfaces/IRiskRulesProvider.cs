using VendorRisk.Domain.Models;

namespace VendorRisk.Domain.Interfaces;

public interface IRiskRulesProvider
{
    Task<RiskRulesConfig> GetAsync(CancellationToken cancellationToken = default);
}
