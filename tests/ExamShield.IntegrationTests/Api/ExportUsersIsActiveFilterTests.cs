using System.Net;
using System.Net.Http.Json;

namespace ExamShield.IntegrationTests.Api;

public sealed class ExportUsersIsActiveFilterTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync() =>
        _client = await factory.CreateAuthenticatedClientAsync();

    public Task DisposeAsync() { _client.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task ExportUsers_NoFilter_ReturnsCsv()
    {
        var res = await _client.GetAsync("/users/export");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("text/csv", res.Content.Headers.ContentType?.MediaType);
        var csv = await res.Content.ReadAsStringAsync();
        Assert.Contains("UserId", csv);
    }

    [Fact]
    public async Task ExportUsers_IsActiveTrue_CsvContainsOnlyActiveUsers()
    {
        // Create one active user and one inactive user
        var activeEmail   = $"active_{Guid.NewGuid():N}@x.com";
        var inactiveEmail = $"inactive_{Guid.NewGuid():N}@x.com";

        var active = await _client.PostAsJsonAsync("/auth/users",
            new { email = activeEmail, password = "Str0ng!Password", role = "Auditor" });
        Assert.Equal(HttpStatusCode.Created, active.StatusCode);

        var inactive = await _client.PostAsJsonAsync("/auth/users",
            new { email = inactiveEmail, password = "Str0ng!Password", role = "Auditor" });
        Assert.Equal(HttpStatusCode.Created, inactive.StatusCode);
        var inactiveBody = await inactive.Content.ReadFromJsonAsync<CreateUserResponse>();
        await _client.PutAsync($"/users/{inactiveBody!.UserId}/deactivate", null);

        // Export only active users
        var res = await _client.GetAsync("/users/export?isActive=true");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var csv = await res.Content.ReadAsStringAsync();

        Assert.Contains(activeEmail, csv);
        Assert.DoesNotContain(inactiveEmail, csv);
    }

    [Fact]
    public async Task ExportUsers_IsActiveFalse_CsvContainsOnlyInactiveUsers()
    {
        var activeEmail   = $"actv_{Guid.NewGuid():N}@x.com";
        var inactiveEmail = $"inactv_{Guid.NewGuid():N}@x.com";

        await _client.PostAsJsonAsync("/auth/users",
            new { email = activeEmail, password = "Str0ng!Password", role = "Auditor" });

        var cr = await _client.PostAsJsonAsync("/auth/users",
            new { email = inactiveEmail, password = "Str0ng!Password", role = "Auditor" });
        var body = await cr.Content.ReadFromJsonAsync<CreateUserResponse>();
        await _client.PutAsync($"/users/{body!.UserId}/deactivate", null);

        var res = await _client.GetAsync("/users/export?isActive=false");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var csv = await res.Content.ReadAsStringAsync();

        Assert.Contains(inactiveEmail, csv);
        Assert.DoesNotContain(activeEmail, csv);
    }

    private sealed record CreateUserResponse(Guid UserId);
}
