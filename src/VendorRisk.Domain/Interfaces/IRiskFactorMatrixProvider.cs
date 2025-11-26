using VendorRisk.Domain.Models;

namespace VendorRisk.Domain.Interfaces;

public interface IRiskFactorMatrixProvider
{
    Task<RiskFactorMatrix> GetMatrixAsync(CancellationToken cancellationToken = default);
}
