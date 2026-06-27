using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;

namespace ExamShield.IntegrationTests.Api;

public sealed class BulkEnrollTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private HttpClient _client = null!;
    private Guid _examId;

    public async Task InitializeAsync()
    {
        _client = await factory.CreateAuthenticatedClientAsync();
        var res  = await _client.PostAsJsonAsync("/exams/",
            new CreateExamRequest("Bulk Enroll Test", null, 10));
        var exam = await res.Content.ReadFromJsonAsync<ExamResponse>();
        _examId  = exam!.ExamId;
    }

    public Task DisposeAsync() { _client.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task BulkEnroll_AllNew_ReturnsCorrectCounts()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        var res  = await _client.PostAsJsonAsync($"/exams/{_examId}/students/bulk",
            new BulkEnrollRequest(ids));
        var body = await res.Content.ReadFromJsonAsync<BulkEnrollResponse>();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(3, body!.Enrolled);
        Assert.Equal(0, body.AlreadyEnrolled);
        Assert.Equal(3, body.Total);
    }

    [Fact]
    public async Task BulkEnroll_SomeDuplicates_SkipsExisting()
    {
        var existing = Guid.NewGuid();
        await _client.PostAsJsonAsync($"/exams/{_examId}/students",
            new EnrollStudentRequest(existing));

        var ids = new[] { existing, Guid.NewGuid() };
        var res  = await _client.PostAsJsonAsync($"/exams/{_examId}/students/bulk",
            new BulkEnrollRequest(ids));
        var body = await res.Content.ReadFromJsonAsync<BulkEnrollResponse>();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(1, body!.Enrolled);
        Assert.Equal(1, body.AlreadyEnrolled);
    }

    [Fact]
    public async Task BulkEnroll_AllDuplicates_Returns0Enrolled()
    {
        var id = Guid.NewGuid();
        await _client.PostAsJsonAsync($"/exams/{_examId}/students",
            new EnrollStudentRequest(id));

        var res  = await _client.PostAsJsonAsync($"/exams/{_examId}/students/bulk",
            new BulkEnrollRequest([id]));
        var body = await res.Content.ReadFromJsonAsync<BulkEnrollResponse>();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(0, body!.Enrolled);
        Assert.Equal(1, body.AlreadyEnrolled);
    }

    [Fact]
    public async Task BulkEnroll_EmptyList_Returns400()
    {
        var res = await _client.PostAsJsonAsync($"/exams/{_examId}/students/bulk",
            new BulkEnrollRequest([]));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task BulkEnroll_UnknownExam_Returns404()
    {
        var res = await _client.PostAsJsonAsync($"/exams/{Guid.NewGuid()}/students/bulk",
            new BulkEnrollRequest([Guid.NewGuid()]));

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task BulkEnroll_Unauthenticated_Returns401()
    {
        var res = await factory.CreateClient().PostAsJsonAsync(
            $"/exams/{_examId}/students/bulk",
            new BulkEnrollRequest([Guid.NewGuid()]));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
