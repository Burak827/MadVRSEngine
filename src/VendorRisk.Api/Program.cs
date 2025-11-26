using Microsoft.EntityFrameworkCore;
using Serilog;
using VendorRisk.Domain.Interfaces;
using VendorRisk.Infrastructure.Data;
using VendorRisk.Infrastructure.Repositories;
using VendorRisk.Infrastructure.Services;

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

builder.Services.AddScoped<IVendorProfileRepository, VendorProfileRepository>();
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

static async Task SeedDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}
