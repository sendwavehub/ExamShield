using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using FluentAssertions;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class ReportEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ReportEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetReportSummary_ReturnsOk()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/reports/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ReportSummaryResponse>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReportSummary_ContainsAllSections()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/reports/summary");
        var body = await response.Content.ReadFromJsonAsync<ReportSummaryResponse>();

        body!.Captures.Should().NotBeNull();
        body.Ocr.Should().NotBeNull();
        body.Scores.Should().NotBeNull();
        body.Security.Should().NotBeNull();
        body.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetReportSummary_CaptureTotalsAreNonNegative()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/reports/summary");
        var body = await response.Content.ReadFromJsonAsync<ReportSummaryResponse>();

        body!.Captures.Total.Should().BeGreaterThanOrEqualTo(0);
        body.Captures.Verified.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetReportSummary_WithoutAuth_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/reports/summary");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
