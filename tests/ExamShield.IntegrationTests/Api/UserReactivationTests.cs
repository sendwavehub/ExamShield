using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;

namespace ExamShield.IntegrationTests.Api;

public sealed class UserReactivationTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private async Task<Guid> CreateDeactivatedUserAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/auth/users",
            new CreateUserRequest(email, "Test@1234", "Operator"));
        var list = await (await client.GetAsync("/users/"))
            .Content.ReadFromJsonAsync<UserListResponse>();
        var user = list!.Users.First(u => u.Email == email);
        await client.PutAsync($"/users/{user.UserId}/deactivate", null);
        return user.UserId;
    }

    [Fact]
    public async Task ActivateUser_DeactivatedUser_Returns204()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var userId = await CreateDeactivatedUserAsync(client, "reactivate-204@examshield.local");

        var res = await client.PutAsync($"/users/{userId}/activate", null);

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task ActivateUser_UserIsActiveAfterReactivation()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var email  = "reactivate-verify@examshield.local";
        var userId = await CreateDeactivatedUserAsync(client, email);

        await client.PutAsync($"/users/{userId}/activate", null);

        var list = await (await client.GetAsync("/users/"))
            .Content.ReadFromJsonAsync<UserListResponse>();
        var user = list!.Users.First(u => u.UserId == userId);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task ActivateUser_AlreadyActive_Returns422()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/auth/users",
            new CreateUserRequest("already-active@examshield.local", "Test@1234", "Operator"));
        var list = await (await client.GetAsync("/users/"))
            .Content.ReadFromJsonAsync<UserListResponse>();
        var user = list!.Users.First(u => u.Email == "already-active@examshield.local");

        var res = await client.PutAsync($"/users/{user.UserId}/activate", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    [Fact]
    public async Task ActivateUser_UnknownId_Returns404()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var res = await client.PutAsync($"/users/{Guid.NewGuid()}/activate", null);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task ActivateUser_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var res = await client.PutAsync($"/users/{Guid.NewGuid()}/activate", null);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
