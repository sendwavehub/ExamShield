using ExamShield.Infrastructure.Security;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class MfaEndpointTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly TotpService _totp = new();

    private Task<HttpClient> AuthedClient() => factory.CreateAuthenticatedClientAsync();

    [Fact]
    public async Task GetMfaStatus_ReturnsDisabledForNewUser()
    {
        var client = await AuthedClient();
        var res = await client.GetAsync("/auth/mfa/status");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<MfaStatusDto>();
        body!.MfaEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task SetupMfa_ReturnsSecretAndQrUri()
    {
        var client = await AuthedClient();
        var res = await client.PostAsync("/auth/mfa/setup", null);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<MfaSetupDto>();
        body!.Secret.Should().MatchRegex("^[A-Z2-7]{32}$");
        body.QrUri.Should().StartWith("otpauth://totp/");
    }

    [Fact]
    public async Task VerifyMfa_WithValidCode_EnablesMfa()
    {
        var client = await AuthedClient();
        var setup = await (await client.PostAsync("/auth/mfa/setup", null))
            .Content.ReadFromJsonAsync<MfaSetupDto>();

        var code = _totp.GenerateCurrentCode(setup!.Secret);
        var res = await client.PostAsJsonAsync("/auth/mfa/verify", new { code });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<MfaStatusDto>();
        body!.MfaEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyMfa_WithInvalidCode_Returns401()
    {
        var client = await AuthedClient();
        await client.PostAsync("/auth/mfa/setup", null);
        var res = await client.PostAsJsonAsync("/auth/mfa/verify", new { code = "000000" });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DisableMfa_SetsMfaEnabledFalse()
    {
        var client = await AuthedClient();
        var setup = await (await client.PostAsync("/auth/mfa/setup", null))
            .Content.ReadFromJsonAsync<MfaSetupDto>();

        var code = _totp.GenerateCurrentCode(setup!.Secret);
        await client.PostAsJsonAsync("/auth/mfa/verify", new { code });

        var res = await client.DeleteAsync("/auth/mfa/");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<MfaStatusDto>();
        body!.MfaEnabled.Should().BeFalse();
    }

    private sealed record MfaStatusDto(bool MfaEnabled);
    private sealed record MfaSetupDto(string Secret, string QrUri);
}
