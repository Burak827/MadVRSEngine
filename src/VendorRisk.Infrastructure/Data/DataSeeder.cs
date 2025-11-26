using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VendorRisk.Domain.Models;

namespace VendorRisk.Infrastructure.Data;

public class DataSeeder
{
    private readonly VendorDbContext _context;
    private readonly ILogger<DataSeeder> _logger;
    private readonly string _seedFilePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public DataSeeder(VendorDbContext context, ILogger<DataSeeder> logger, string seedFilePath)
    {
        _context = context;
        _logger = logger;
        _seedFilePath = seedFilePath;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.EnsureCreatedAsync(cancellationToken);

        if (await _context.VendorProfiles.AnyAsync(cancellationToken))
        {
            return;
        }

        if (!File.Exists(_seedFilePath))
        {
            _logger.LogWarning("Seed file not found at {Path}. Skipping data seeding.", _seedFilePath);
            return;
        }

        await using var stream = File.OpenRead(_seedFilePath);
        var seedData = await JsonSerializer.DeserializeAsync<SeedVendors>(stream, JsonOptions, cancellationToken);
        if (seedData?.Vendors is null || seedData.Vendors.Count == 0)
        {
            _logger.LogWarning("Seed file {Path} is empty or invalid.", _seedFilePath);
            return;
        }

        _context.VendorProfiles.AddRange(seedData.Vendors);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} vendor profiles.", seedData.Vendors.Count);
    }

    private class SeedVendors
    {
        public List<VendorProfile> Vendors { get; set; } = new();
    }
}
