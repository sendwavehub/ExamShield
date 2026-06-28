using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using ExamShield.Domain.Enums;
using FluentAssertions;

namespace ExamShield.IntegrationTests.Api;

/// <summary>
/// A student must not be able to submit a review request using another student's identity —
/// i.e., PUT body.StudentId must match the authenticated caller's JWT sub.
/// </summary>
public sealed class SubmitReviewRequestIdentityTests
    : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _adminClient = null!;
    private Guid _examId;

    public SubmitReviewRequestIdentityTests(TestWebApplicationFactory factory) =>
        _factory = factory;

    public async Task InitializeAsync()
    {
        _adminClient = await _factory.CreateAuthenticatedClientAsync();
        _examId = await _factory.CreateActivatedExamAsync("Review Identity Exam", totalQuestions: 5);
    }

    public Task DisposeAsync() { _adminClient.Dispose(); return Task.CompletedTask; }

    private sealed record CreateUserReq(string Email, string Password, string Role);
    private sealed record LoginReq(string Email, string Password);
    private sealed record LoginResp(string Token, string RefreshToken, string Role);

    private async Task<(HttpClient Client, Guid UserId)> CreateStudentClientAsync()
    {
        var email = $"rq-ident-{Guid.NewGuid():N}@test.com";
        await _adminClient.PostAsJsonAsync("/auth/users",
            new CreateUserReq(email, "StudentPass1!", "Student"));
        var anon = _factory.CreateClient();
        var loginRes = await anon.PostAsJsonAsync("/auth/login", new LoginReq(email, "StudentPass1!"));
        var login = await loginRes.Content.ReadFromJsonAsync<LoginResp>();
        anon.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login!.Token);
        var sub = new JwtSecurityTokenHandler().ReadJwtToken(login.Token)
            .Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        return (anon, Guid.Parse(sub));
    }

    [Fact]
    public async Task SubmitReviewRequest_WithAnotherStudentsIdentity_Returns403()
    {
        // Student A owns a capture registered on their behalf
        var (clientA, userIdA) = await CreateStudentClientAsync();
        var (clientB, _)       = await CreateStudentClientAsync();

        // Enroll student A and register a capture for them
        await _adminClient.PostAsJsonAsync($"/exams/{_examId}/students",
            new EnrollStudentRequest(userIdA));
        var (captureId, _) = await TestHelpers.RegisterCaptureForStudentAsync(
            _adminClient, _examId, userIdA);

        // Student B submits a review request claiming to be student A
        var res = await clientB.PostAsJsonAsync("/student/review-request", new
        {
            captureId,
            studentId = userIdA,   // ← body claims to be student A
            reason = "Impersonation attempt"
        });

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        clientA.Dispose();
        clientB.Dispose();
    }

    [Fact]
    public async Task SubmitReviewRequest_WithOwnIdentity_Returns201()
    {
        var (client, userId) = await CreateStudentClientAsync();

        await _adminClient.PostAsJsonAsync($"/exams/{_examId}/students",
            new EnrollStudentRequest(userId));
        var (captureId, _) = await TestHelpers.RegisterCaptureForStudentAsync(
            _adminClient, _examId, userId);

        var res = await client.PostAsJsonAsync("/student/review-request", new
        {
            captureId,
            studentId = userId,
            reason = "Please review my answer"
        });

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        client.Dispose();
    }
}
