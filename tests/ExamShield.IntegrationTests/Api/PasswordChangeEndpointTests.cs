using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class PasswordChangeEndpointTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_Returns204()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        var res = await client.PostAsJsonAsync("/auth/password/change", new
        {
            currentPassword = TestWebApplicationFactory.AdminPassword,
            newPassword = "NewPass@9876!",
        });
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns401()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        var res = await client.PostAsJsonAsync("/auth/password/change", new
        {
            currentPassword = "WrongPassword!99",
            newPassword = "NewPass@9876!",
        });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WhenUnauthenticated_Returns401()
    {
        using var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/password/change", new
        {
            currentPassword = TestWebApplicationFactory.AdminPassword,
            newPassword = "NewPass@9876!",
        });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
