using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VendorRisk.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vendor_profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FinancialHealth = table.Column<int>(type: "integer", nullable: false),
                    SlaUptime = table.Column<int>(type: "integer", nullable: false),
                    MajorIncidents = table.Column<int>(type: "integer", nullable: false),
                    SecurityCerts = table.Column<string>(type: "text", nullable: false),
                    contract_valid = table.Column<bool>(type: "boolean", nullable: false),
                    privacy_policy_valid = table.Column<bool>(type: "boolean", nullable: false),
                    pentest_report_valid = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_profiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vendor_profiles");
        }
    }
}
