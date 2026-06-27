using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using FluentAssertions;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class UserEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UserEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetUsers_ReturnsOkWithList()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/users/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserListResponse>();
        body!.Users.Should().NotBeNull();
        body.Users.Should().Contain(u => u.Email == TestWebApplicationFactory.AdminEmail);
    }

    [Fact]
    public async Task GetUsers_WithoutAuth_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/users/");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutUserRole_UnknownUser_Returns404()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/users/{Guid.NewGuid()}/role",
            new UpdateUserRoleRequest("Operator"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutDeactivateUser_UnknownUser_Returns404()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsync($"/users/{Guid.NewGuid()}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateThenDeactivate_UserIsMarkedInactive()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create a new user
        var create = await client.PostAsJsonAsync("/auth/users",
            new CreateUserRequest("deactivate-test@test.com", "Str0ng!Pass", "Operator"));
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        // Find the user id
        var list = await (await client.GetAsync("/users/"))
            .Content.ReadFromJsonAsync<UserListResponse>();
        var user = list!.Users.First(u => u.Email == "deactivate-test@test.com");

        // Deactivate
        var deactivate = await client.PutAsync($"/users/{user.UserId}/deactivate", null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify
        var updated = await (await client.GetAsync("/users/"))
            .Content.ReadFromJsonAsync<UserListResponse>();
        updated!.Users.First(u => u.UserId == user.UserId).IsActive.Should().BeFalse();
    }
}
