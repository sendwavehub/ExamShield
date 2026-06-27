using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using ExamShield.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class MfaLoginEndpointTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TotpService _totp = new();

    [Fact]
    public async Task Login_WhenMfaNotEnabled_ReturnTokensDirectly()
    {
        using var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail,
                             TestWebApplicationFactory.AdminPassword));
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
        body!.RequiresMfa.Should().BeFalse();
        body.Token.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WhenMfaEnabled_ReturnsRequiresMfaWithNoToken()
    {
        // Enable MFA on admin user
        var authedClient = await factory.CreateAuthenticatedClientAsync();
        var setup = await (await authedClient.PostAsync("/auth/mfa/setup", null))
            .Content.ReadFromJsonAsync<MfaSetupDto>();
        var code = _totp.GenerateCurrentCode(setup!.Secret);
        await authedClient.PostAsJsonAsync("/auth/mfa/verify", new { code });

        // Fresh client — login without MFA code
        using var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail,
                             TestWebApplicationFactory.AdminPassword));
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
        body!.RequiresMfa.Should().BeTrue();
        body.Token.Should().BeNullOrWhiteSpace();

        // Cleanup — disable MFA so other tests aren't affected
        await authedClient.DeleteAsync("/auth/mfa/");
    }

    [Fact]
    public async Task MfaLogin_WithCorrectCode_ReturnsFullTokens()
    {
        // Enable MFA
        var authedClient = await factory.CreateAuthenticatedClientAsync();
        var setup = await (await authedClient.PostAsync("/auth/mfa/setup", null))
            .Content.ReadFromJsonAsync<MfaSetupDto>();
        var setupCode = _totp.GenerateCurrentCode(setup!.Secret);
        await authedClient.PostAsJsonAsync("/auth/mfa/verify", new { code = setupCode });

        // Complete MFA login
        using var client = factory.CreateClient();
        var mfaCode = _totp.GenerateCurrentCode(setup.Secret);
        var res = await client.PostAsJsonAsync("/auth/mfa/login",
            new MfaLoginRequest(TestWebApplicationFactory.AdminEmail,
                                TestWebApplicationFactory.AdminPassword,
                                mfaCode));

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.RequiresMfa.Should().BeFalse();

        await authedClient.DeleteAsync("/auth/mfa/");
    }

    [Fact]
    public async Task MfaLogin_WithInvalidCode_Returns401()
    {
        // Enable MFA
        var authedClient = await factory.CreateAuthenticatedClientAsync();
        var setup = await (await authedClient.PostAsync("/auth/mfa/setup", null))
            .Content.ReadFromJsonAsync<MfaSetupDto>();
        var setupCode = _totp.GenerateCurrentCode(setup!.Secret);
        await authedClient.PostAsJsonAsync("/auth/mfa/verify", new { code = setupCode });

        using var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/mfa/login",
            new MfaLoginRequest(TestWebApplicationFactory.AdminEmail,
                                TestWebApplicationFactory.AdminPassword,
                                "000000"));

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await authedClient.DeleteAsync("/auth/mfa/");
    }

    private sealed record MfaSetupDto(string Secret, string QrUri);
}
