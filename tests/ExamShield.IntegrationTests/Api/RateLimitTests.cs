using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using FluentAssertions;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

/// <summary>
/// Rate limit tests use RateLimitTestFactory which injects tight limits (3 req/1-second window)
/// via in-memory config, overriding the high defaults in appsettings.Testing.json.
/// A fresh factory per class ensures state never bleeds into other test classes.
/// </summary>
public sealed class RateLimitTests : IAsyncLifetime
{
    private readonly RateLimitTestFactory _factory = new();
    private HttpClient _anon = null!;
    private HttpClient _auth = null!;

    public async Task InitializeAsync()
    {
        _anon = _factory.CreateClient();
        _auth = await _factory.CreateAuthenticatedClientAsync();
    }

    public async Task DisposeAsync()
    {
        _anon.Dispose();
        _auth.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task AuthLogin_NormalRequest_Returns200Or401()
    {
        var response = await _anon.PostAsJsonAsync("/auth/login",
            new LoginRequest("user@test.com", "wrongpassword"));

        // Rate limiter should not block a single request
        response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task AuthLogin_ExceedingLimit_Returns429()
    {
        // Auth limit in Testing = 3 per 1-second window → 4th request gets 429
        var req = new LoginRequest("flood@test.com", "wrongpass");

        for (int i = 0; i < 3; i++)
            await _anon.PostAsJsonAsync("/auth/login", req);

        var response = await _anon.PostAsJsonAsync("/auth/login", req);
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task AuthLogin_RateLimitedResponse_HasRetryAfterHeader()
    {
        var req = new LoginRequest("header@test.com", "wrongpass");

        for (int i = 0; i < 3; i++)
            await _anon.PostAsJsonAsync("/auth/login", req);

        var response = await _anon.PostAsJsonAsync("/auth/login", req);
        response.Headers.Should().ContainKey("Retry-After");
    }

    [Fact]
    public async Task UploadEndpoint_ExceedingLimit_Returns429()
    {
        // Upload limit in Testing = 3 per 1-second window
        for (int i = 0; i < 3; i++)
            await _auth.PostAsJsonAsync("/upload", new UploadImageRequest(Guid.NewGuid(), new byte[1]));

        var response = await _auth.PostAsJsonAsync("/upload", new UploadImageRequest(Guid.NewGuid(), new byte[1]));
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GeneralApi_NormalVolume_IsNotRateLimited()
    {
        // 5 requests well within the global limit
        for (int i = 0; i < 5; i++)
        {
            var r = await _auth.GetAsync("/devices");
            r.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }
    }
}
