using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;

namespace ExamShield.IntegrationTests.Api;

public sealed class GetUserByIdTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private HttpClient _client = null!;
    private Guid _adminId;

    public async Task InitializeAsync()
    {
        _client = await factory.CreateAuthenticatedClientAsync();

        var list = await (await _client.GetAsync("/users/")).Content
            .ReadFromJsonAsync<UserListResponse>();
        _adminId = list!.Users.First().UserId;
    }

    public Task DisposeAsync() { _client.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task GetUserById_KnownId_ReturnsUser()
    {
        var res = await _client.GetAsync($"/users/{_adminId}");
        res.EnsureSuccessStatusCode();
        var user = await res.Content.ReadFromJsonAsync<UserDetailResponse>();
        Assert.Equal(_adminId, user!.UserId);
        Assert.NotEmpty(user.Email);
        Assert.NotEmpty(user.Role);
    }

    [Fact]
    public async Task GetUserById_UnknownId_Returns404()
    {
        var res = await _client.GetAsync($"/users/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task UpdateUserProfile_SetsDisplayName()
    {
        var res = await _client.PutAsJsonAsync($"/users/{_adminId}",
            new UpdateUserProfileRequest("Test Admin Display"));
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        var user = await (await _client.GetAsync($"/users/{_adminId}"))
            .Content.ReadFromJsonAsync<UserDetailResponse>();
        Assert.Equal("Test Admin Display", user!.DisplayName);
    }

    [Fact]
    public async Task UpdateUserProfile_ClearsDisplayName()
    {
        await _client.PutAsJsonAsync($"/users/{_adminId}", new UpdateUserProfileRequest("Temp Name"));

        var res = await _client.PutAsJsonAsync($"/users/{_adminId}", new UpdateUserProfileRequest(null));
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        var user = await (await _client.GetAsync($"/users/{_adminId}"))
            .Content.ReadFromJsonAsync<UserDetailResponse>();
        Assert.Null(user!.DisplayName);
    }

    [Fact]
    public async Task UpdateUserProfile_UnknownId_Returns404()
    {
        var res = await _client.PutAsJsonAsync($"/users/{Guid.NewGuid()}",
            new UpdateUserProfileRequest("X"));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task GetUserById_Unauthenticated_Returns401()
    {
        var res = await factory.CreateClient().GetAsync($"/users/{_adminId}");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
