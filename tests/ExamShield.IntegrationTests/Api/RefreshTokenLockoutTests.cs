using System.Net;
using System.Net.Http.Json;

namespace ExamShield.IntegrationTests.Api;

public sealed class RefreshTokenLockoutTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private sealed record CreateUserReq(string Email, string Password, string Role);
    private sealed record LoginRequest(string Email, string Password);
    private sealed record LoginResponse(string Token, string RefreshToken, string Role);
    private sealed record RefreshRequest(string RefreshToken);

    [Fact]
    public async Task RefreshToken_WhenAccountIsLockedOut_Returns401()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync();
        using var anon  = factory.CreateClient();

        var email    = $"refresh-lock-{Guid.NewGuid():N}@test.com";
        const string password = "RefreshLock1!";
        await admin.PostAsJsonAsync("/auth/users", new CreateUserReq(email, password, "Operator"));

        var loginRes = await anon.PostAsJsonAsync("/auth/login",
            new LoginRequest(email, password));
        var loginBody = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginBody?.RefreshToken);

        for (var i = 0; i < 5; i++)
            await anon.PostAsJsonAsync("/auth/login", new LoginRequest(email, "Wrong!"));

        var refreshRes = await anon.PostAsJsonAsync("/auth/refresh",
            new RefreshRequest(loginBody!.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, refreshRes.StatusCode);
    }
}
