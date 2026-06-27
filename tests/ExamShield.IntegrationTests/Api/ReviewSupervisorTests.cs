using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace ExamShield.IntegrationTests.Api;

public sealed class ReviewSupervisorTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private async Task<Guid> SeedCompletedReviewAsync()
    {
        using var scope = factory.Services.CreateScope();
        var reviewRepo = scope.ServiceProvider.GetRequiredService<IManualReviewRepository>();
        var ocrRepo    = scope.ServiceProvider.GetRequiredService<IOcrResultRepository>();

        var captureId = CaptureId.New();
        var answers   = new[] { new ExtractedAnswer(1, "A", new OcrConfidence(0.3)) };
        var ocrResult = OcrResult.Create(captureId, answers);
        await ocrRepo.AddAsync(ocrResult);

        var review = ManualReview.CreateFor(ocrResult);
        review.Complete([new ReviewedAnswer(1, "B")], UserId.New());
        await reviewRepo.AddAsync(review);

        return review.Id.Value;
    }

    [Fact]
    public async Task ApproveReview_CompletedReview_Returns204()
    {
        var client   = await factory.CreateAuthenticatedClientAsync();
        var reviewId = await SeedCompletedReviewAsync();

        var res = await client.PutAsync($"/reviews/{reviewId}/approve", null);

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task ApproveReview_UnknownId_Returns404()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var res = await client.PutAsync($"/reviews/{Guid.NewGuid()}/approve", null);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task RejectReview_CompletedReview_Returns204()
    {
        var client   = await factory.CreateAuthenticatedClientAsync();
        var reviewId = await SeedCompletedReviewAsync();

        var res = await client.PutAsJsonAsync($"/reviews/{reviewId}/reject",
            new RejectReviewRequest("Answers appear inconsistent"));

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task RejectReview_UnknownId_Returns404()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var res = await client.PutAsJsonAsync($"/reviews/{Guid.NewGuid()}/reject",
            new RejectReviewRequest("Some reason"));

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task ApproveReview_WhenUnauthenticated_Returns401()
    {
        var client   = factory.CreateClient();
        var reviewId = await SeedCompletedReviewAsync();

        var res = await client.PutAsync($"/reviews/{reviewId}/approve", null);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task ApproveReview_AlreadyApproved_Returns422()
    {
        var client   = await factory.CreateAuthenticatedClientAsync();
        var reviewId = await SeedCompletedReviewAsync();

        await client.PutAsync($"/reviews/{reviewId}/approve", null);
        var res = await client.PutAsync($"/reviews/{reviewId}/approve", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }
}
