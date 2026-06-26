using ExamShield.Api.Contracts;
using ExamShield.Application.Queries.ServerVerifyCapture;
using MediatR;

namespace ExamShield.Api.Endpoints;

public static class VerifyEndpoints
{
    public static IEndpointRouteBuilder MapVerifyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/verify/{id:guid}", ServerVerifyAsync)
            .WithName("ServerVerifyCapture")
            .WithTags("Verify")
            .RequireAuthorization("Auditor")
            .Produces<ServerVerifyResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ServerVerifyAsync(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new ServerVerifyCaptureQuery(id), ct);

        return Results.Ok(new ServerVerifyResponse(
            result.IsValid, result.HashValid, result.SignatureValid,
            result.StorageKey, result.ExpectedHash, result.ActualHash));
    }
}
