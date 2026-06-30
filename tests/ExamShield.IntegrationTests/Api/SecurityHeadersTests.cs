using FluentAssertions;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

/// <summary>
/// Verifies that <see cref="ExamShield.Api.Middleware.SecurityHeadersMiddleware"/>
/// injects all required security headers on every response, regardless of endpoint or status code.
/// Tests run against GET /health (no auth required) so they are independent of auth state.
/// </summary>
public sealed class SecurityHeadersTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Response_HasXContentTypeOptionsNoSniff()
    {
        var response = await _client.GetAsync("/health");
        response.Headers.TryGetValues("X-Content-Type-Options", out var values);
        values.Should().ContainSingle().Which.Should().Be("nosniff");
    }

    [Fact]
    public async Task Response_HasXFrameOptionsDeny()
    {
        var response = await _client.GetAsync("/health");
        response.Headers.TryGetValues("X-Frame-Options", out var values);
        values.Should().ContainSingle().Which.Should().Be("DENY");
    }

    [Fact]
    public async Task Response_HasXXssProtectionZero()
    {
        var response = await _client.GetAsync("/health");
        response.Headers.TryGetValues("X-XSS-Protection", out var values);
        values.Should().ContainSingle().Which.Should().Be("0");
    }

    [Fact]
    public async Task Response_HasReferrerPolicy()
    {
        var response = await _client.GetAsync("/health");
        response.Headers.TryGetValues("Referrer-Policy", out var values);
        values.Should().ContainSingle().Which.Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task Response_HasPermissionsPolicy()
    {
        var response = await _client.GetAsync("/health");
        response.Headers.TryGetValues("Permissions-Policy", out var values);
        values.Should().ContainSingle().Which.Should()
            .Contain("camera=()")
            .And.Contain("microphone=()")
            .And.Contain("geolocation=()");
    }

    [Fact]
    public async Task Response_HasContentSecurityPolicy()
    {
        var response = await _client.GetAsync("/health");
        response.Headers.TryGetValues("Content-Security-Policy", out var values);
        values.Should().ContainSingle().Which.Should()
            .Contain("default-src 'self'")
            .And.Contain("frame-ancestors 'none'")
            .And.Contain("object-src 'none'");
    }

    [Fact]
    public async Task Response_DoesNotHaveStrictTransportSecurity_OnHttp()
    {
        // TestServer uses HTTP — HSTS must not be sent over plain HTTP.
        var response = await _client.GetAsync("/health");
        response.Headers.Contains("Strict-Transport-Security").Should().BeFalse();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/auth/login")]
    [InlineData("/public/verify")]
    public async Task Response_SecurityHeadersPresentOnMultipleEndpoints(string path)
    {
        var response = await _client.GetAsync(path);
        response.Headers.Contains("X-Content-Type-Options").Should().BeTrue(
            $"X-Content-Type-Options must be set on {path}");
        response.Headers.Contains("X-Frame-Options").Should().BeTrue(
            $"X-Frame-Options must be set on {path}");
        response.Headers.Contains("Content-Security-Policy").Should().BeTrue(
            $"Content-Security-Policy must be set on {path}");
    }
}
