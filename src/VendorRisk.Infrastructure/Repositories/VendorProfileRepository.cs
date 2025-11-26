using Microsoft.EntityFrameworkCore;
using VendorRisk.Domain.Interfaces;
using VendorRisk.Domain.Models;
using VendorRisk.Infrastructure.Data;

namespace VendorRisk.Infrastructure.Repositories;

public class VendorProfileRepository : IVendorProfileRepository
{
    private readonly VendorDbContext _context;

    public VendorProfileRepository(VendorDbContext context)
    {
        _context = context;
    }

    public async Task<VendorProfile?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.VendorProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<VendorProfile>> ListAsync(CancellationToken cancellationToken = default)
    {
        var vendors = await _context.VendorProfiles
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return vendors;
    }

    public async Task<VendorProfile> AddAsync(VendorProfile vendor, CancellationToken cancellationToken = default)
    {
        await _context.VendorProfiles.AddAsync(vendor, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return vendor;
    }

    public async Task UpdateAsync(VendorProfile vendor, CancellationToken cancellationToken = default)
    {
        _context.VendorProfiles.Update(vendor);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(VendorProfile vendor, CancellationToken cancellationToken = default)
    {
        _context.VendorProfiles.Remove(vendor);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
