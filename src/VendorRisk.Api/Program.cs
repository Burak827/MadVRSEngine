using Microsoft.EntityFrameworkCore;
using Serilog;
using VendorRisk.Domain.Interfaces;
using VendorRisk.Infrastructure.Data;
using VendorRisk.Infrastructure.Options;
using VendorRisk.Infrastructure.Repositories;
using VendorRisk.Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

ConfigureDatabase(builder);
ConfigureCaching(builder);

builder.Services.AddScoped<VendorProfileRepository>();
builder.Services.AddScoped<IVendorProfileRepository>(sp =>
{
    var baseRepo = sp.GetRequiredService<VendorProfileRepository>();
    var cacheOptions = sp.GetRequiredService<IOptions<CacheOptions>>().Value;
    if (!cacheOptions.UseRedis)
    {
        return baseRepo;
    }

    var cache = sp.GetRequiredService<IDistributedCache>();
    var logger = sp.GetRequiredService<ILogger<CachedVendorProfileRepository>>();
    return new CachedVendorProfileRepository(baseRepo, cache, sp.GetRequiredService<IOptions<CacheOptions>>(), logger);
});
builder.Services.AddScoped<IRiskEngine, RuleEngineService>();
builder.Services.AddSingleton<IRiskFactorMatrixProvider>(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    var logger = sp.GetRequiredService<ILogger<FileRiskFactorMatrixProvider>>();
    var filePath = Path.Combine(env.ContentRootPath, builder.Configuration["Data:RiskMatrixPath"] ?? "Data/RiskFactorMatrix.json");
    return new FileRiskFactorMatrixProvider(filePath, logger);
});

builder.Services.AddScoped<DataSeeder>(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    var logger = sp.GetRequiredService<ILogger<DataSeeder>>();
    var ctx = sp.GetRequiredService<VendorDbContext>();
    var seedPath = Path.Combine(env.ContentRootPath, builder.Configuration["Data:SeedPath"] ?? "Data/SampleVendorData.json");
    return new DataSeeder(ctx, logger, seedPath);
});

var app = builder.Build();

await SeedDatabaseAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.MapControllers();

await app.RunAsync();

static void ConfigureDatabase(WebApplicationBuilder builder)
{
    var provider = builder.Configuration.GetValue<string>("DatabaseProvider")?.ToLowerInvariant() ?? "inmemory";
    builder.Services.AddDbContext<VendorDbContext>(options =>
    {
        if (provider is "postgres" or "postgresql")
        {
            var connectionString = builder.Configuration.GetConnectionString("VendorDatabase")
                                   ?? "Host=localhost;Port=5432;Database=vendorrisk;Username=postgres;Password=postgres";
            options.UseNpgsql(connectionString);
        }
        else
        {
            options.UseInMemoryDatabase("VendorRiskDb");
        }
    });
}

static void ConfigureCaching(WebApplicationBuilder builder)
{
    builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Cache"));
    var cacheSection = builder.Configuration.GetSection("Cache");
    var useRedis = cacheSection.GetValue<bool>("UseRedis");
    if (useRedis)
    {
        var redisConnection = cacheSection.GetValue<string>("RedisConnection") ?? "localhost:6379";
        builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
    }
}

static async Task SeedDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}
