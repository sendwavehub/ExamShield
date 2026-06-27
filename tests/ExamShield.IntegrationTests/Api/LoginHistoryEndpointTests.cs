using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using FluentAssertions;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class LoginHistoryEndpointTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task GetLoginHistory_AfterSuccessfulLogin_ReturnsLoginSuccessEntry()
    {
        // Trigger a login to generate a history entry
        using var anon = factory.CreateClient();
        await anon.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail,
                             TestWebApplicationFactory.AdminPassword));

        using var client = await factory.CreateAuthenticatedClientAsync();
        var res = await client.GetAsync("/security/login-history");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<LoginHistoryResponse>();
        body!.Events.Should().NotBeEmpty();
        body.Events.Should().Contain(e => e.EventType == "LoginSuccess");
    }

    [Fact]
    public async Task GetLoginHistory_AfterFailedLogin_ReturnsLoginFailedEntry()
    {
        using var anon = factory.CreateClient();
        await anon.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail, "WrongPassword!"));

        using var client = await factory.CreateAuthenticatedClientAsync();
        var res = await client.GetAsync("/security/login-history");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<LoginHistoryResponse>();
        body!.Events.Should().Contain(e => e.EventType == "LoginFailed");
    }

    [Fact]
    public async Task GetLoginHistory_WhenUnauthenticated_Returns401()
    {
        using var client = factory.CreateClient();
        var res = await client.GetAsync("/security/login-history");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record LoginHistoryResponse(List<LoginHistoryEntry> Events);
    private sealed record LoginHistoryEntry(string EventType, string? IpAddress, DateTimeOffset OccurredAt);
}
