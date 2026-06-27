using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using FluentAssertions;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class SettingsEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SettingsEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetSettings_ReturnsDefaults()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/settings/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SettingsResponse>();
        body!.OcrConfidenceThreshold.Should().BeGreaterThan(0);
        body.AccessTokenExpiryMinutes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateSettings_PersistsChanges()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var update = new UpdateSettingsRequest(
            OcrConfidenceThreshold: 0.92,
            NotificationsEnabled: false,
            NotificationSeverity: "Critical",
            AccessTokenExpiryMinutes: 30,
            RefreshTokenExpiryDays: 14);

        var putResponse = await client.PutAsJsonAsync("/settings/", update);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync("/settings/");
        var body = await getResponse.Content.ReadFromJsonAsync<SettingsResponse>();
        body!.OcrConfidenceThreshold.Should().BeApproximately(0.92, 0.001);
        body.NotificationsEnabled.Should().BeFalse();
        body.AccessTokenExpiryMinutes.Should().Be(30);
    }

    [Fact]
    public async Task GetSettings_WithoutAuth_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/settings/");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateSettings_WithoutAuth_Returns401()
    {
        var response = await _factory.CreateClient()
            .PutAsJsonAsync("/settings/", new UpdateSettingsRequest(0.9, true, "High", 60, 7));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
