# Vendor Risk Scoring Engine (Rule-Based Edition)

Rule-based, explainable vendor risk scorer aligned to the case study brief. Generates a composite `riskScore`, categorical `riskLevel`, and human-readable reasons across financial, operational, and security/compliance dimensions.

## Quickstart
- Restore & run API (defaults to in-memory DB and ships with seed vendors):  
  `dotnet run --project src/VendorRisk.Api`
- Explore OpenAPI/Swagger: `http://localhost:5000/swagger` (or the HTTPS port shown in console).
- Run tests: `dotnet test MadVRSEngine.sln`
- Docker (API + PostgreSQL + Redis): `docker-compose up --build`  
  API: `http://localhost:8080/swagger`, DB: `localhost:5432` (`vendorrisk` / `postgres` / `postgres`), Redis: `localhost:6379`.

## Configuration
- `DatabaseProvider`: `InMemory` (default) or `Postgres` (for real DB). Override via environment variable.
- `ConnectionStrings__VendorDatabase`: PostgreSQL connection string (used when provider = Postgres).
- Dataset paths (copied to output): `Data:RiskMatrixPath` -> `Data/RiskFactorMatrix.json`, `Data:SeedPath` -> `Data/SampleVendorData.json`.
- Caching: set `Cache:UseRedis=true` and `Cache:RedisConnection` (e.g., `localhost:6379`) to enable vendor lookup caching; TTL via `Cache:TtlSeconds` (default 300s). Falls back to in-memory cache when disabled.
- Logging: Serilog JSON console via `appsettings*.json` (compact JSON formatter, request logging enabled).

## Domain & Architecture
- **Domain (`VendorRisk.Domain`)**: `VendorProfile`, `VendorDocuments`, `RiskAssessment` (+ `RiskBreakdown`, `RiskLevel`), interfaces for `IRiskEngine`, `IRiskFactorMatrixProvider`, `IVendorProfileRepository`.
- **Infrastructure (`VendorRisk.Infrastructure`)**: EF Core `VendorDbContext`, `VendorProfileRepository`, `CachedVendorProfileRepository` (Redis decorator), `FileRiskFactorMatrixProvider`, `RuleEngineService` (rule-based scorer), `DataSeeder` (loads seed vendors).
- **API (`VendorRisk.Api`)**: ASP.NET Core controllers, DI wiring, Serilog, Swagger/OpenAPI, dataset copy to output.
- **Tests (`VendorRisk.Tests`)**: xUnit + Moq + FluentAssertions covering rule engine scenarios.

## Endpoints
- `GET /api/vendors` - list vendors (seeded from `SampleVendorData.json`).
- `GET /api/vendors/{id}` - fetch vendor.
- `POST /api/vendors` - create vendor (body = `VendorProfile` shape).
- `PUT /api/vendors/{id}` - replace vendor.
- `DELETE /api/vendors/{id}` - remove vendor.
- `GET /api/vendors/{id}/risk` - compute risk assessment with explanations.

Example: `GET /api/vendors/1/risk`
```json
{
  "vendorId": 1,
  "riskScore": 0.74,
  "riskLevel": "High",
  "breakdown": { "financialRisk": 0.45, "operationalRisk": 0.62, "securityComplianceRisk": 0.77 },
  "reasons": [
    "Missing ISO27001 certification elevates security risk.",
    "Privacy policy expired or missing.",
    "Related risk patterns: downtime (0.87), slowTicketResolution (0.83)"
  ]
}
```

## Scoring Model
- Formula: `FinalScore = (FinancialRisk * 0.4) + (OperationalRisk * 0.3) + (SecurityComplianceRisk * 0.3)`.
- Rules (selected):
  - Financial: `<50` -> high (0.85), `>80` -> low (0.25), mid bands -> moderate.
  - Operational: SLA `<95%` + incidents increase risk; SLA `>99%` reduces risk.
  - Security/Compliance: missing ISO27001, expired privacy policy, or missing/failed pentest each add risk; no certs adds a small penalty.
- Similarity matrix: `Data/RiskFactorMatrix.json` used to enrich explanations with related risk patterns.

## Data
- Seed vendors: `src/VendorRisk.Api/Data/SampleVendorData.json` (15 vendors with financial/operational/security fields).
- Similarity matrix: `src/VendorRisk.Api/Data/RiskFactorMatrix.json`.

## Docker Notes
- API listens on `8080`; PostgreSQL exposed on `5432`; Redis on `6379`.
- `docker-compose.yml` wires API to Postgres and Redis (`DatabaseProvider=Postgres`, `Cache__UseRedis=true`).

## What's Next (not implemented)
- UI/dashboard for vendor comparison.
- Seasonal/industry-specific risk modifiers.
