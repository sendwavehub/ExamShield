using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class ProfileEndpointTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task GetProfile_ReturnsEmailAndRole()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        var res = await client.GetAsync("/auth/profile");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<ProfileDto>();
        body!.Email.Should().Be(TestWebApplicationFactory.AdminEmail);
        body.Role.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetProfile_WhenUnauthenticated_Returns401()
    {
        using var client = factory.CreateClient();
        (await client.GetAsync("/auth/profile")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSessions_ReturnsList()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        var res = await client.GetAsync("/auth/sessions");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<SessionsDto>();
        body!.Sessions.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSessions_WhenUnauthenticated_Returns401()
    {
        using var client = factory.CreateClient();
        (await client.GetAsync("/auth/sessions")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeSession_WithInvalidId_Returns404()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        var res = await client.DeleteAsync($"/auth/sessions/{Guid.NewGuid()}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record ProfileDto(string Email, string Role, bool MfaEnabled);
    private sealed record SessionDto(Guid Id, DateTimeOffset ExpiresAt);
    private sealed record SessionsDto(List<SessionDto> Sessions);
}
