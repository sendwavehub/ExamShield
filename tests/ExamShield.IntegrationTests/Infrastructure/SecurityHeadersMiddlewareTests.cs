using ExamShield.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ExamShield.IntegrationTests.Infrastructure;

/// <summary>
/// Unit-style tests for <see cref="SecurityHeadersMiddleware"/> using DefaultHttpContext
/// so no HTTP round-trip is required. Complements SecurityHeadersTests (which hits a live
/// test server) by testing the HTTPS-only HSTS branch without needing TLS setup.
/// </summary>
public sealed class SecurityHeadersMiddlewareTests
{
    private static async Task<IHeaderDictionary> InvokeAsync(bool isHttps = false)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.IsHttps = isHttps;
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(ctx);
        return ctx.Response.Headers;
    }

    [Fact]
    public async Task Sets_XContentTypeOptions_Nosniff()
    {
        var headers = await InvokeAsync();
        headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
    }

    [Fact]
    public async Task Sets_XFrameOptions_Deny()
    {
        var headers = await InvokeAsync();
        headers["X-Frame-Options"].ToString().Should().Be("DENY");
    }

    [Fact]
    public async Task Sets_XXssProtection_Zero()
    {
        var headers = await InvokeAsync();
        headers["X-XSS-Protection"].ToString().Should().Be("0");
    }

    [Fact]
    public async Task Sets_ReferrerPolicy()
    {
        var headers = await InvokeAsync();
        headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task Sets_PermissionsPolicy_WithCameraAndMicrophone()
    {
        var headers = await InvokeAsync();
        var pp = headers["Permissions-Policy"].ToString();
        pp.Should().Contain("camera=()").And.Contain("microphone=()").And.Contain("payment=()");
    }

    [Fact]
    public async Task Sets_ContentSecurityPolicy_WithSelfAndNoFrameAncestors()
    {
        var headers = await InvokeAsync();
        var csp = headers["Content-Security-Policy"].ToString();
        csp.Should().Contain("default-src 'self'")
            .And.Contain("frame-ancestors 'none'")
            .And.Contain("object-src 'none'");
    }

    [Fact]
    public async Task Http_DoesNotSet_Hsts()
    {
        var headers = await InvokeAsync(isHttps: false);
        headers.ContainsKey("Strict-Transport-Security").Should().BeFalse();
    }

    [Fact]
    public async Task Https_Sets_Hsts_WithPreload()
    {
        var headers = await InvokeAsync(isHttps: true);
        var hsts = headers["Strict-Transport-Security"].ToString();
        hsts.Should().Contain("max-age=31536000")
            .And.Contain("includeSubDomains")
            .And.Contain("preload");
    }

    [Fact]
    public async Task InvokeAsync_AlwaysCallsNextDelegate()
    {
        var ctx     = new DefaultHttpContext();
        var called  = false;
        var middleware = new SecurityHeadersMiddleware(_ => { called = true; return Task.CompletedTask; });
        await middleware.InvokeAsync(ctx);
        called.Should().BeTrue();
    }
}
