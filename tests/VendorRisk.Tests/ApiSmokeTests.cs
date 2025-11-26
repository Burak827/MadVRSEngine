using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using VendorRisk.Api;
using VendorRisk.Domain.Models;

namespace VendorRisk.Tests;

public class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task GetRisk_ReturnsAssessment_ForSeedVendor()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/vendors/1/risk");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<RiskAssessment>();
        payload.Should().NotBeNull();
        payload!.RiskLevel.Should().NotBe(default);
        payload.RiskScore.Should().BeGreaterThan(0);
        payload.Reasons.Should().NotBeEmpty();
    }
}
