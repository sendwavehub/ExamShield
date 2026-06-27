using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;

namespace ExamShield.IntegrationTests.Api;

public sealed class ScoreExportTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private Guid _examId;

    public ScoreExportTests(TestWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();

        var examRes = await _client.PostAsJsonAsync("/exams/", new CreateExamRequest("Export Score Exam", null, 1));
        var exam = await examRes.Content.ReadFromJsonAsync<ExamResponse>();
        _examId = exam!.ExamId;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExportScores_NoFilter_ReturnsCSVWithHeader()
    {
        var res = await _client.GetAsync("/score/export");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("text/csv", res.Content.Headers.ContentType?.MediaType);
        var csv = await res.Content.ReadAsStringAsync();
        Assert.Contains("ScoreId", csv);
        Assert.Contains("Percentage", csv);
    }

    [Fact]
    public async Task ExportScores_WithExamIdFilter_Returns200()
    {
        var res = await _client.GetAsync($"/score/export?examId={_examId}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var csv = await res.Content.ReadAsStringAsync();
        Assert.Contains("ScoreId", csv);
    }

    [Fact]
    public async Task ExportScores_WithUnknownExamId_ReturnsEmptyCSV()
    {
        var res = await _client.GetAsync($"/score/export?examId={Guid.NewGuid()}");
        var csv = await res.Content.ReadAsStringAsync();

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines); // header only
    }

    [Fact]
    public async Task ExportScores_Unauthenticated_Returns401()
    {
        var anon = _factory.CreateClient();
        var res = await anon.GetAsync("/score/export");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
