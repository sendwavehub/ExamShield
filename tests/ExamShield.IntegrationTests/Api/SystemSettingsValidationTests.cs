using System.Net;
using System.Net.Http.Json;

namespace ExamShield.IntegrationTests.Api;

public sealed class SystemSettingsValidationTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private sealed record UpdateRequest(
        double OcrConfidenceThreshold,
        bool NotificationsEnabled,
        string NotificationSeverity,
        int AccessTokenExpiryMinutes,
        int RefreshTokenExpiryDays);

    [Theory]
    [InlineData(1.5)]
    [InlineData(-0.1)]
    public async Task UpdateSettings_InvalidThreshold_Returns400(double threshold)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var res = await client.PutAsJsonAsync("/settings",
            new UpdateRequest(threshold, true, "High", 60, 7));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.85)]
    [InlineData(1.0)]
    public async Task UpdateSettings_ValidThreshold_Returns200(double threshold)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var res = await client.PutAsJsonAsync("/settings",
            new UpdateRequest(threshold, true, "High", 60, 7));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateSettings_InvalidAccessTokenExpiry_Returns400(int minutes)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var res = await client.PutAsJsonAsync("/settings",
            new UpdateRequest(0.85, true, "High", minutes, 7));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
