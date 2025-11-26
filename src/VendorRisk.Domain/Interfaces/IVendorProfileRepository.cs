using VendorRisk.Domain.Models;

namespace VendorRisk.Domain.Interfaces;

public interface IVendorProfileRepository
{
    Task<VendorProfile?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<VendorProfile>> ListAsync(CancellationToken cancellationToken = default);
    Task<VendorProfile> AddAsync(VendorProfile vendor, CancellationToken cancellationToken = default);
    Task UpdateAsync(VendorProfile vendor, CancellationToken cancellationToken = default);
    Task DeleteAsync(VendorProfile vendor, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
