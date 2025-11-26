using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VendorRisk.Domain.Models;

namespace VendorRisk.Infrastructure.Data;

public class VendorDbContext : DbContext
{
    public VendorDbContext(DbContextOptions<VendorDbContext> options) : base(options)
    {
    }

    public DbSet<VendorProfile> VendorProfiles => Set<VendorProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var certConverter = new ValueConverter<List<string>, string>(
            value => JsonSerializer.Serialize(value ?? new List<string>(), new JsonSerializerOptions()),
            value => string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(value, new JsonSerializerOptions()) ?? new List<string>());

        var certComparer = new ValueComparer<List<string>>(
            (c1, c2) => (c1 ?? new List<string>()).SequenceEqual(c2 ?? new List<string>()),
            c => (c ?? new List<string>()).Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => (c ?? new List<string>()).ToList());

        modelBuilder.Entity<VendorProfile>(builder =>
        {
            builder.ToTable("vendor_profiles");
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Name)
                .IsRequired()
                .HasMaxLength(200);

            // Basic scalar props.
            builder.Property(v => v.FinancialHealth).IsRequired();
            builder.Property(v => v.SlaUptime).IsRequired();
            builder.Property(v => v.MajorIncidents).IsRequired();

            // Store cert list as JSON string to keep schema simple.
            builder.Property(v => v.SecurityCerts)
                .HasConversion(certConverter)
                .Metadata.SetValueComparer(certComparer);

            builder.OwnsOne(v => v.Documents, docs =>
            {
                docs.Property(d => d.ContractValid).HasColumnName("contract_valid");
                docs.Property(d => d.PrivacyPolicyValid).HasColumnName("privacy_policy_valid");
                docs.Property(d => d.PentestReportValid).HasColumnName("pentest_report_valid");
            });
        });
    }
}
