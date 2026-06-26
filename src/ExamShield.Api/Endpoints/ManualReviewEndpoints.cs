using System.Security.Claims;
using ExamShield.Api.Contracts;
using ExamShield.Application.Commands.SubmitReview;
using ExamShield.Application.Queries.GetPendingReviews;
using MediatR;

namespace ExamShield.Api.Endpoints;

public static class ManualReviewEndpoints
{
    public static void MapManualReviewEndpoints(this WebApplication app)
    {
        app.MapGet("/reviews", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPendingReviewsQuery(), ct);
            var items = result.Reviews
                .Select(r => new PendingReviewItem(r.ReviewId, r.CaptureId, r.OcrResultId, r.CreatedAt))
                .ToList();
            return Results.Ok(new GetPendingReviewsResponse(items));
        })
        .RequireAuthorization("Operator");

        app.MapPost("/reviews/{reviewId:guid}/submit", async (
            Guid reviewId,
            SubmitReviewRequest request,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var answers = request.Answers
                .Select(a => new ReviewedAnswerDto(a.QuestionNumber, a.Text))
                .ToList();
            await mediator.Send(new SubmitReviewCommand(reviewId, answers, userId), ct);
            return Results.Ok();
        })
        .RequireAuthorization("Operator");
    }
}
