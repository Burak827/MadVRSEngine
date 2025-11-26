using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendorRisk.Domain.Interfaces;
using VendorRisk.Domain.Models;
using VendorRisk.Infrastructure.Options;

namespace VendorRisk.Infrastructure.Repositories;

public class CachedVendorProfileRepository : IVendorProfileRepository
{
    private readonly IVendorProfileRepository _inner;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedVendorProfileRepository> _logger;
    private readonly CacheOptions _options;
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public CachedVendorProfileRepository(
        IVendorProfileRepository inner,
        IDistributedCache cache,
        IOptions<CacheOptions> options,
        ILogger<CachedVendorProfileRepository> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<VendorProfile?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKey(id);
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<VendorProfile>(cached, SerializerOptions);
        }

        var vendor = await _inner.GetAsync(id, cancellationToken);
        if (vendor is not null)
        {
            await SetAsync(cacheKey, vendor, cancellationToken);
        }

        return vendor;
    }

    public Task<IReadOnlyCollection<VendorProfile>> ListAsync(CancellationToken cancellationToken = default)
    {
        // For simplicity keep list uncached to avoid stale enumerations.
        return _inner.ListAsync(cancellationToken);
    }

    public async Task<VendorProfile> AddAsync(VendorProfile vendor, CancellationToken cancellationToken = default)
    {
        var created = await _inner.AddAsync(vendor, cancellationToken);
        await InvalidateAsync(created.Id, cancellationToken);
        return created;
    }

    public async Task UpdateAsync(VendorProfile vendor, CancellationToken cancellationToken = default)
    {
        await _inner.UpdateAsync(vendor, cancellationToken);
        await InvalidateAsync(vendor.Id, cancellationToken);
    }

    public async Task DeleteAsync(VendorProfile vendor, CancellationToken cancellationToken = default)
    {
        await _inner.DeleteAsync(vendor, cancellationToken);
        await InvalidateAsync(vendor.Id, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _inner.SaveChangesAsync(cancellationToken);

    private async Task SetAsync(string key, VendorProfile vendor, CancellationToken cancellationToken)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.TtlSeconds)
            };
            var payload = JsonSerializer.Serialize(vendor, SerializerOptions);
            await _cache.SetStringAsync(key, payload, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache vendor {VendorId}", vendor.Id);
        }
    }

    private Task InvalidateAsync(int vendorId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey(vendorId);
        return _cache.RemoveAsync(cacheKey, cancellationToken);
    }

    private static string CacheKey(int id) => $"vendor:{id}";
}
