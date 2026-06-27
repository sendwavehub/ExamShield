using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using ExamShield.Application.RolePermissions;
using FluentAssertions;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class RoleEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public RoleEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetRoles_ReturnsAllDefinedRoles()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RoleListResponse>();
        body!.Roles.Should().HaveCount(RolePermissionDefinitions.All.Count);
    }

    [Fact]
    public async Task GetRoles_EachRoleHasPermissions()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/roles");
        var body = await response.Content.ReadFromJsonAsync<RoleListResponse>();

        body!.Roles.Should().AllSatisfy(r => r.Permissions.Should().NotBeEmpty());
    }

    [Fact]
    public async Task GetRoles_ContainsAdministratorRole()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/roles");
        var body = await response.Content.ReadFromJsonAsync<RoleListResponse>();

        body!.Roles.Should().Contain(r => r.RoleName == "Administrator");
    }

    [Fact]
    public async Task GetRoles_WithoutAuth_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/roles");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
