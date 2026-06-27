using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using FluentAssertions;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class DashboardStatsTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync() =>
        _client = await factory.CreateAuthenticatedClientAsync();

    public Task DisposeAsync() { _client.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task GetDashboardStats_ReturnsOk()
    {
        var response = await _client.GetAsync("/dashboard/stats");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDashboardStats_ReturnsExpectedShape()
    {
        var response = await _client.GetAsync("/dashboard/stats");
        var body = await response.Content.ReadFromJsonAsync<DashboardStatsResponse>();

        body.Should().NotBeNull();
        body!.TotalCaptures.Should().BeGreaterThanOrEqualTo(0);
        body.PendingReview.Should().BeGreaterThanOrEqualTo(0);
        body.VerifiedToday.Should().BeGreaterThanOrEqualTo(0);
        body.ActiveAlerts.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetDashboardStats_Unauthenticated_Returns401()
    {
        var anon = factory.CreateClient();
        var response = await anon.GetAsync("/dashboard/stats");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
