using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using ExamShield.Api.Contracts;
using FluentAssertions;

namespace ExamShield.IntegrationTests.Api;

public sealed class ManualReviewEndpointsTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private readonly ECDsa _ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    // Low-confidence stub: we can't control what the stub OCR returns,
    // so we test the GET endpoints and the scenario where no reviews exist.

    public ManualReviewEndpointsTests(TestWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync() =>
        _client = await _factory.CreateAuthenticatedClientAsync();

    public Task DisposeAsync()
    {
        _ecdsa.Dispose();
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetPendingReviews_Returns200()
    {
        var response = await _client.GetAsync("/reviews");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPendingReviews_ReturnsListShape()
    {
        var response = await _client.GetAsync("/reviews");
        var body = await response.Content.ReadFromJsonAsync<GetPendingReviewsResponse>();
        body!.Reviews.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPendingReviews_Unauthenticated_Returns401()
    {
        using var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/reviews");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostSubmitReview_WithUnknownId_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/reviews/{Guid.NewGuid()}/submit",
            new SubmitReviewRequest([new ReviewedAnswerRequest(1, "A")]));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostSubmitReview_Unauthenticated_Returns401()
    {
        using var unauthClient = _factory.CreateClient();
        var response = await unauthClient.PostAsJsonAsync(
            $"/reviews/{Guid.NewGuid()}/submit",
            new SubmitReviewRequest([new ReviewedAnswerRequest(1, "A")]));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
