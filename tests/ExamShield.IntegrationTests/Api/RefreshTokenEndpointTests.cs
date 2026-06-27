using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using FluentAssertions;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class RefreshTokenEndpointTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task Login_ResponseIncludesRefreshToken()
    {
        using var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail,
                             TestWebApplicationFactory.AdminPassword));
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
        body!.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewAccessAndRefreshToken()
    {
        using var client = factory.CreateClient();
        var loginRes = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail,
                             TestWebApplicationFactory.AdminPassword));
        var login = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();

        var res = await client.PostAsJsonAsync("/auth/refresh",
            new RefreshRequest(login!.RefreshToken));
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBe(login.RefreshToken); // token rotation
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns401()
    {
        using var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/refresh",
            new RefreshRequest("invalid-token-value"));
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        using var client = factory.CreateClient();
        var loginRes = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail,
                             TestWebApplicationFactory.AdminPassword));
        var login = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();

        // Set bearer for logout
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login!.Token);

        await client.PostAsJsonAsync("/auth/logout",
            new RefreshRequest(login.RefreshToken));

        // Refresh should now fail
        using var client2 = factory.CreateClient();
        var res = await client2.PostAsJsonAsync("/auth/refresh",
            new RefreshRequest(login.RefreshToken));
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_AfterTokenUsed_Returns401()
    {
        using var client = factory.CreateClient();
        var loginRes = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail,
                             TestWebApplicationFactory.AdminPassword));
        var login = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();

        // First refresh succeeds
        await client.PostAsJsonAsync("/auth/refresh", new RefreshRequest(login!.RefreshToken));

        // Second refresh with same (rotated-out) token fails
        var res = await client.PostAsJsonAsync("/auth/refresh",
            new RefreshRequest(login.RefreshToken));
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
