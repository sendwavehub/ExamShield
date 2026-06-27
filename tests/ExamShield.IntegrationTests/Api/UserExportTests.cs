using System.Net;

namespace ExamShield.IntegrationTests.Api;

public sealed class UserExportTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task ExportUsers_NoFilter_ReturnsCsv()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/users/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("UserId", csv);
        Assert.Contains("Email", csv);
        Assert.Contains("Role", csv);
    }

    [Fact]
    public async Task ExportUsers_WithSearchFilter_ReturnsFilteredCsv()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/users/export?search=admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("admin", csv);
    }

    [Fact]
    public async Task ExportUsers_WithRoleFilter_ReturnsOnlyThatRole()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/users/export?role=Administrator");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var csv = await response.Content.ReadAsStringAsync();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines.Skip(1)) // skip header
            Assert.Contains("Administrator", line);
    }

    [Fact]
    public async Task ExportUsers_ContentDispositionHeaderSet()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/users/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var disposition = response.Content.Headers.ContentDisposition;
        Assert.NotNull(disposition);
        Assert.EndsWith(".csv", disposition!.FileName?.Trim('"'));
    }

    [Fact]
    public async Task ExportUsers_Unauthenticated_Returns401()
    {
        var anon = factory.CreateClient();
        var response = await anon.GetAsync("/users/export");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
