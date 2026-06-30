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

    // ── HttpOnly cookie tests ─────────────────────────────────────────────────

    [Fact]
    public async Task Login_SetsHttpOnlyRefreshTokenCookie()
    {
        using var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail,
                             TestWebApplicationFactory.AdminPassword));

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var setCookie = res.Headers.GetValues("Set-Cookie").SingleOrDefault(v => v.Contains("rt="));
        setCookie.Should().NotBeNullOrEmpty();
        var lc = setCookie!.ToLowerInvariant();
        lc.Should().Contain("httponly");
        lc.Should().Contain("samesite=strict");
        lc.Should().Contain("path=/auth");
    }

    [Fact]
    public async Task Refresh_WithCookieOnly_ReturnsNewTokensAndRotatesCookie()
    {
        // Use a CookieContainer so the test client forwards the login cookie on refresh.
        var handler = new HttpClientHandler { UseCookies = true };
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var loginRes = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail,
                             TestWebApplicationFactory.AdminPassword));
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Send an empty body — no refreshToken field — cookie should be used.
        var refreshRes = await client.PostAsJsonAsync("/auth/refresh", new RefreshRequest(null));
        refreshRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await refreshRes.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();

        // Response must rotate the cookie.
        var setCookie = refreshRes.Headers.GetValues("Set-Cookie").SingleOrDefault(v => v.Contains("rt="));
        setCookie.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Logout_ClearsRefreshTokenCookie()
    {
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var loginRes = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail,
                             TestWebApplicationFactory.AdminPassword));
        var login = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login!.Token);

        // Logout with empty body — server reads cookie.
        var logoutRes = await client.PostAsJsonAsync("/auth/logout", new RefreshRequest(null));
        logoutRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Cookie should be cleared (Max-Age=0 or expired).
        var setCookie = logoutRes.Headers.GetValues("Set-Cookie").SingleOrDefault(v => v.Contains("rt="));
        setCookie.Should().NotBeNullOrEmpty();
        setCookie.Should().MatchRegex(@"(?i)(max-age=0|expires=.*1970)");
    }

    [Fact]
    public async Task Refresh_AfterTokenUsed_Returns401()
    {
        // Disable cookie handling so the rotated cookie is not forwarded on the second call —
        // this tests body-token rotation (the mobile-client fallback path).
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
        });
        var loginRes = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail,
                             TestWebApplicationFactory.AdminPassword));
        var login = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();

        // First refresh succeeds (body token)
        await client.PostAsJsonAsync("/auth/refresh", new RefreshRequest(login!.RefreshToken));

        // Second refresh with same (rotated-out) body token fails
        var res = await client.PostAsJsonAsync("/auth/refresh",
            new RefreshRequest(login.RefreshToken));
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
