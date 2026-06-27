using System.Security.Claims;
using ExamShield.Api.Contracts;
using ExamShield.Application.Commands.ApproveReview;
using ExamShield.Application.Commands.EscalateReview;
using ExamShield.Application.Commands.RejectReview;
using ExamShield.Application.Commands.SubmitReview;
using ExamShield.Application.Queries.GetPendingReviews;
using ExamShield.Application.Queries.GetReviewDetail;
using ExamShield.Domain.Exceptions;
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

        app.MapGet("/reviews/{reviewId:guid}", async (
            Guid reviewId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetReviewDetailQuery(reviewId), ct);
            var response = new ReviewDetailResponse(
                result.ReviewId, result.CaptureId, result.OcrResultId, result.Status,
                result.OcrAnswers.Select(a => new OcrAnswerResponse(a.QuestionNumber, a.Text, a.Confidence)).ToList(),
                result.CreatedAt);
            return Results.Ok(response);
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

        app.MapPut("/reviews/{reviewId:guid}/approve", async (
            Guid reviewId, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var supervisorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                await mediator.Send(new ApproveReviewCommand(reviewId, supervisorId), ct);
                return Results.NoContent();
            }
            catch (ManualReviewNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("ApproveReview")
        .WithTags("Review")
        .RequireAuthorization("Supervisor")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPut("/reviews/{reviewId:guid}/reject", async (
            Guid reviewId, RejectReviewRequest request,
            ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var supervisorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                await mediator.Send(new RejectReviewCommand(reviewId, supervisorId, request.Reason), ct);
                return Results.NoContent();
            }
            catch (ManualReviewNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("RejectReview")
        .WithTags("Review")
        .RequireAuthorization("Supervisor")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPut("/reviews/{reviewId:guid}/escalate", async (
            Guid reviewId, EscalateReviewRequest request,
            ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var supervisorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                await mediator.Send(new EscalateReviewCommand(reviewId, supervisorId, request.Reason), ct);
                return Results.NoContent();
            }
            catch (ManualReviewNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("EscalateReview")
        .WithTags("Review")
        .RequireAuthorization("Supervisor")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
