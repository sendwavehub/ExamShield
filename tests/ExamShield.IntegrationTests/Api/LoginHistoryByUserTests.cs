using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;

namespace ExamShield.IntegrationTests.Api;

public sealed class LoginHistoryByUserTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private sealed record LoginHistoryResponse(List<LoginEvent> Events);
    private sealed record LoginEvent(string? UserId);

    [Fact]
    public async Task GetLoginHistory_WithUserId_ReturnsOnlyThatUsersEvents()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        // Create two distinct users
        var email1 = $"lh1-{Guid.NewGuid()}@test.com";
        var email2 = $"lh2-{Guid.NewGuid()}@test.com";

        var r1 = await client.PostAsJsonAsync("/auth/users",
            new { email = email1, password = "Str0ng!Password", role = "Operator" });
        var user1 = await r1.Content.ReadFromJsonAsync<UserResponse>();

        await client.PostAsJsonAsync("/auth/users",
            new { email = email2, password = "Str0ng!Password", role = "Operator" });

        // Generate login events for user1 only
        await client.PostAsJsonAsync("/auth/login",
            new { email = email1, password = "Str0ng!Password" });

        var res = await client.GetAsync(
            $"/security/login-history?userId={user1!.UserId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<LoginHistoryResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.Events);
        Assert.All(body.Events, e => Assert.Equal(user1.UserId.ToString(), e.UserId));
    }

    [Fact]
    public async Task GetLoginHistory_WithUnknownUserId_ReturnsEmpty()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var res = await client.GetAsync($"/security/login-history?userId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<LoginHistoryResponse>();
        Assert.Empty(body!.Events);
    }
}
