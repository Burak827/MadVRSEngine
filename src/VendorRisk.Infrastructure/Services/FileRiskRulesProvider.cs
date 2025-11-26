using System.Text.Json;
using Microsoft.Extensions.Logging;
using VendorRisk.Domain.Interfaces;
using VendorRisk.Domain.Models;

namespace VendorRisk.Infrastructure.Services;

public class FileRiskRulesProvider : IRiskRulesProvider
{
    private readonly string _filePath;
    private readonly ILogger<FileRiskRulesProvider> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private RiskRulesConfig? _cached;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileRiskRulesProvider(string filePath, ILogger<FileRiskRulesProvider> logger)
    {
        _filePath = filePath;
        _logger = logger;
    }

    public async Task<RiskRulesConfig> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cached is not null)
            {
                return _cached;
            }

            if (!File.Exists(_filePath))
            {
                _logger.LogWarning("Risk rules file not found at {Path}. Using defaults.", _filePath);
                _cached = new RiskRulesConfig();
                return _cached;
            }

            // Load tunable thresholds/weights from JSON.
            await using var stream = File.OpenRead(_filePath);
            var rules = await JsonSerializer.DeserializeAsync<RiskRulesConfig>(stream, JsonOptions, cancellationToken);
            _cached = rules ?? new RiskRulesConfig();
            return _cached;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load risk rules from {Path}. Using defaults.", _filePath);
            _cached = new RiskRulesConfig();
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }
}
