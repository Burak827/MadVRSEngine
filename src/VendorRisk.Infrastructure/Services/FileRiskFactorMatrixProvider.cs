using System.Text.Json;
using Microsoft.Extensions.Logging;
using VendorRisk.Domain.Interfaces;
using VendorRisk.Domain.Models;

namespace VendorRisk.Infrastructure.Services;

public class FileRiskFactorMatrixProvider : IRiskFactorMatrixProvider
{
    private readonly string _filePath;
    private readonly ILogger<FileRiskFactorMatrixProvider> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private RiskFactorMatrix? _cachedMatrix;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileRiskFactorMatrixProvider(string filePath, ILogger<FileRiskFactorMatrixProvider> logger)
    {
        _filePath = filePath;
        _logger = logger;
    }

    public async Task<RiskFactorMatrix> GetMatrixAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedMatrix is not null)
        {
            return _cachedMatrix;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedMatrix is not null)
            {
                return _cachedMatrix;
            }

            if (!File.Exists(_filePath))
            {
                _logger.LogWarning("Risk matrix file not found at {Path}. Using empty matrix.", _filePath);
                _cachedMatrix = new RiskFactorMatrix();
                return _cachedMatrix;
            }

            await using var stream = File.OpenRead(_filePath);
            var matrix = await JsonSerializer.DeserializeAsync<RiskFactorMatrix>(stream, JsonOptions, cancellationToken);
            _cachedMatrix = matrix ?? new RiskFactorMatrix();
            return _cachedMatrix;
        }
        finally
        {
            _lock.Release();
        }
    }
}
