using ExamShield.Api.Contracts;
using ExamShield.Application.Commands.TriggerOcr;
using ExamShield.Application.Queries.GetOcrResult;
using ExamShield.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ExamShield.Api.Endpoints;

public static class OcrEndpoints
{
    public static void MapOcrEndpoints(this WebApplication app)
    {
        app.MapPost("/ocr/{captureId:guid}", async (
            Guid captureId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new TriggerOcrCommand(captureId), ct);
            return Results.Ok(new TriggerOcrResponse(
                result.OcrResultId, result.Status.ToString(), result.RequiresManualReview));
        })
        .RequireAuthorization("Operator");

        app.MapGet("/ocr/{captureId:guid}", async (
            Guid captureId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetOcrResultQuery(captureId), ct);
            return Results.Ok(new GetOcrResultResponse(
                result.OcrResultId, result.CaptureId, result.Status.ToString(),
                result.OverallConfidence, result.RequiresManualReview,
                result.Answers.Select(a => new OcrAnswerResponse(a.QuestionNumber, a.Text, a.Confidence)).ToList()));
        })
        .RequireAuthorization("Operator");
    }
}
