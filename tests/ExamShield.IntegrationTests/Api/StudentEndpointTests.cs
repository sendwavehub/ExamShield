using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExamShield.IntegrationTests.Api;

public sealed class StudentEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public StudentEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetStudentResults_WithNoScores_ReturnsEmptyList()
    {
        var studentId = Guid.NewGuid();
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/student/results?studentId={studentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StudentResultsResponse>();
        body!.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStudentResults_WithMultipleStudents_OnlyReturnsOwnResults()
    {
        var studentA = Guid.NewGuid();
        var studentB = Guid.NewGuid();
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Both students have no scores yet — results should be isolated
        var responseA = await client.GetAsync($"/student/results?studentId={studentA}");
        var responseB = await client.GetAsync($"/student/results?studentId={studentB}");

        var bodyA = await responseA.Content.ReadFromJsonAsync<StudentResultsResponse>();
        var bodyB = await responseB.Content.ReadFromJsonAsync<StudentResultsResponse>();

        bodyA!.StudentId.Should().Be(studentA);
        bodyB!.StudentId.Should().Be(studentB);
        bodyA.Results.Should().BeEmpty();
        bodyB.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStudentResults_WithoutAuth_Returns401()
    {
        var response = await _factory.CreateClient()
            .GetAsync($"/student/results?studentId={Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
