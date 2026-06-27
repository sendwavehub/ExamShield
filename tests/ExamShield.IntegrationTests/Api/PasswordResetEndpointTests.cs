using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;

namespace ExamShield.IntegrationTests.Api;

public sealed class PasswordResetEndpointTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task ForgotPassword_KnownEmail_Returns204()
    {
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/auth/password/forgot",
            new ForgotPasswordRequest(TestWebApplicationFactory.AdminEmail));

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_Returns204()
    {
        // Must not reveal whether the email exists (user enumeration prevention)
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/auth/password/forgot",
            new ForgotPasswordRequest("nobody@example.com"));

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_Returns204()
    {
        var client = factory.CreateClient();
        var token  = await factory.RequestPasswordResetTokenAsync(TestWebApplicationFactory.AdminEmail);

        var res = await client.PostAsJsonAsync("/auth/password/reset",
            new ResetPasswordRequest(token, "NewPassword@9876"));

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_AllowsLoginWithNewPassword()
    {
        var client = factory.CreateClient();
        var token  = await factory.RequestPasswordResetTokenAsync(TestWebApplicationFactory.AdminEmail);

        await client.PostAsJsonAsync("/auth/password/reset",
            new ResetPasswordRequest(token, "NewPassword@9876"));

        var loginRes = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail, "NewPassword@9876"));

        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_Returns400()
    {
        var client       = factory.CreateClient();
        var expiredToken = await factory.RequestExpiredPasswordResetTokenAsync(TestWebApplicationFactory.AdminEmail);

        var res = await client.PostAsJsonAsync("/auth/password/reset",
            new ResetPasswordRequest(expiredToken, "AnyPassword@123"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns400()
    {
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/auth/password/reset",
            new ResetPasswordRequest("not-a-real-token", "AnyPassword@123"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_TokenUsedTwice_SecondAttemptReturns400()
    {
        var client = factory.CreateClient();
        var token  = await factory.RequestPasswordResetTokenAsync(TestWebApplicationFactory.AdminEmail);

        await client.PostAsJsonAsync("/auth/password/reset",
            new ResetPasswordRequest(token, "First@Password1"));

        var secondAttempt = await client.PostAsJsonAsync("/auth/password/reset",
            new ResetPasswordRequest(token, "Second@Password2"));

        Assert.Equal(HttpStatusCode.BadRequest, secondAttempt.StatusCode);
    }
}
