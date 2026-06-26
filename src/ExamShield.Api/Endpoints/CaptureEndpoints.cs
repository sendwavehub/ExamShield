using ExamShield.Api.Contracts;
using ExamShield.Application.Commands.RegisterCapture;
using ExamShield.Application.Commands.VerifyIntegrity;
using MediatR;

namespace ExamShield.Api.Endpoints;

public static class CaptureEndpoints
{
    public static IEndpointRouteBuilder MapCaptureEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/capture").WithTags("Capture");

        group.MapPost("/", RegisterCaptureAsync)
            .WithName("RegisterCapture")
            .RequireAuthorization("Operator")
            .Produces<RegisterCaptureResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPost("/{id:guid}/verify", VerifyIntegrityAsync)
            .WithName("VerifyCapture")
            .RequireAuthorization("Operator")
            .Produces<VerifyIntegrityResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> RegisterCaptureAsync(
        RegisterCaptureRequest request, ISender sender, CancellationToken ct)
    {
        var command = new RegisterCaptureCommand(
            request.ExamId, request.StudentId, request.DeviceId,
            request.PageNumber, request.HashHex, request.SignatureBytes);

        var result = await sender.Send(command, ct);

        return Results.Created(
            $"/capture/{result.CaptureId}",
            new RegisterCaptureResponse(result.CaptureId));
    }

    private static async Task<IResult> VerifyIntegrityAsync(
        Guid id, VerifyIntegrityRequest request, ISender sender, CancellationToken ct)
    {
        var command = new VerifyIntegrityCommand(id, request.ImageBytes);
        var result = await sender.Send(command, ct);

        return Results.Ok(new VerifyIntegrityResponse(
            result.IsValid, result.ExpectedHash, result.ActualHash));
    }
}
