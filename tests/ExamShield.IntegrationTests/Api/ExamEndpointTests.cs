using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using FluentAssertions;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class ExamEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ExamEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetExams_ReturnsOkWithList()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/exams/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ExamListResponse>();
        body!.Exams.Should().NotBeNull();
    }

    [Fact]
    public async Task PostExam_ValidRequest_Returns201WithExam()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var request = new CreateExamRequest("Science Finals 2026", "Annual science exam", 40);

        var response = await client.PostAsJsonAsync("/exams/", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ExamResponse>();
        body!.Name.Should().Be("Science Finals 2026");
        body.Status.Should().Be("Draft");
        body.TotalQuestions.Should().Be(40);
    }

    [Fact]
    public async Task PostExam_ThenGetExams_ContainsNewExam()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var request = new CreateExamRequest("History Test", null, 25);

        await client.PostAsJsonAsync("/exams/", request);
        var listResponse = await client.GetAsync("/exams/");
        var body = await listResponse.Content.ReadFromJsonAsync<ExamListResponse>();

        body!.Exams.Should().Contain(e => e.Name == "History Test");
    }

    [Fact]
    public async Task PostExam_WithZeroQuestions_Returns400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var request = new CreateExamRequest("Bad Exam", null, 0);

        var response = await client.PostAsJsonAsync("/exams/", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetExams_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/exams/");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
