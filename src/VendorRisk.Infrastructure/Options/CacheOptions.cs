namespace VendorRisk.Infrastructure.Options;

public class CacheOptions
{
    public bool UseRedis { get; set; }
    public string? RedisConnection { get; set; }
    public int TtlSeconds { get; set; } = 300;
}
