using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using FluentAssertions;

namespace ExamShield.IntegrationTests.Api;

public sealed class AuthEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── POST /auth/login ───────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidAdminCredentials_Returns200WithToken()
    {
        var response = await _client.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail, TestWebApplicationFactory.AdminPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.Role.Should().Be("Administrator");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/auth/login",
            new LoginRequest(TestWebApplicationFactory.AdminEmail, "wrong-password"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/auth/login",
            new LoginRequest("nobody@test.com", "pw"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /auth/users ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_AsAdmin_Returns201()
    {
        var token = await _factory.GetAuthTokenAsync();
        var authed = _factory.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await authed.PostAsJsonAsync("/auth/users",
            new CreateUserRequest("operator@test.com", "Pass123!", "Operator"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateUser_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/auth/users",
            new CreateUserRequest("op@test.com", "Pass123!", "Operator"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_Returns409()
    {
        var token = await _factory.GetAuthTokenAsync();
        var authed = _factory.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateUserRequest("dup@test.com", "Pass123!", "Operator");
        await authed.PostAsJsonAsync("/auth/users", request);

        var response = await authed.PostAsJsonAsync("/auth/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── Protected endpoints without auth ──────────────────────────────────

    [Fact]
    public async Task PostDevices_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/devices",
            new RegisterDeviceRequest("Dev", new byte[] { 0x04 }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostCapture_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/capture",
            new RegisterCaptureRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                1, new string('a', 64), new byte[64]));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
