using System.Net;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace ExamShield.IntegrationTests.Api;

public sealed class ReviewEscalationTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private async Task<Guid> SeedCompletedReviewAsync()
    {
        using var scope     = factory.Services.CreateScope();
        var reviewRepo      = scope.ServiceProvider.GetRequiredService<IManualReviewRepository>();
        var ocrRepo         = scope.ServiceProvider.GetRequiredService<IOcrResultRepository>();

        var ocrResult = OcrResult.Create(CaptureId.New(),
            [new ExtractedAnswer(1, "A", new OcrConfidence(0.3))]);
        await ocrRepo.AddAsync(ocrResult);

        var review = ManualReview.CreateFor(ocrResult);
        review.Complete([new ReviewedAnswer(1, "B")], UserId.New());
        await reviewRepo.AddAsync(review);
        return review.Id.Value;
    }

    [Fact]
    public async Task EscalateReview_CompletedReview_Returns204()
    {
        var client   = await factory.CreateAuthenticatedClientAsync();
        var reviewId = await SeedCompletedReviewAsync();

        var res = await client.PutAsJsonAsync($"/reviews/{reviewId}/escalate",
            new EscalateReviewRequest("Handwriting is ambiguous — needs senior reviewer"));

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task EscalateReview_UnknownId_Returns404()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var res = await client.PutAsJsonAsync($"/reviews/{Guid.NewGuid()}/escalate",
            new EscalateReviewRequest("Some dispute"));

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task EscalateReview_WhenUnauthenticated_Returns401()
    {
        var client   = factory.CreateClient();
        var reviewId = await SeedCompletedReviewAsync();

        var res = await client.PutAsJsonAsync($"/reviews/{reviewId}/escalate",
            new EscalateReviewRequest("reason"));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task EscalateReview_AlreadyEscalated_Returns422()
    {
        var client   = await factory.CreateAuthenticatedClientAsync();
        var reviewId = await SeedCompletedReviewAsync();

        await client.PutAsJsonAsync($"/reviews/{reviewId}/escalate",
            new EscalateReviewRequest("First escalation"));

        var res = await client.PutAsJsonAsync($"/reviews/{reviewId}/escalate",
            new EscalateReviewRequest("Second escalation attempt"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    [Fact]
    public async Task EscalateReview_PendingReview_Returns422()
    {
        using var scope    = factory.Services.CreateScope();
        var reviewRepo     = scope.ServiceProvider.GetRequiredService<IManualReviewRepository>();
        var ocrRepo        = scope.ServiceProvider.GetRequiredService<IOcrResultRepository>();

        var ocrResult = OcrResult.Create(CaptureId.New(),
            [new ExtractedAnswer(1, "A", new OcrConfidence(0.3))]);
        await ocrRepo.AddAsync(ocrResult);

        var pendingReview = ManualReview.CreateFor(ocrResult); // not completed
        await reviewRepo.AddAsync(pendingReview);

        var client = await factory.CreateAuthenticatedClientAsync();
        var res    = await client.PutAsJsonAsync($"/reviews/{pendingReview.Id.Value}/escalate",
            new EscalateReviewRequest("Cannot escalate pending review"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }
}
