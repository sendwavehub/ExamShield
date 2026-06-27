using ExamShield.Api.Contracts;
using ExamShield.Application.Queries.PublicVerifyCapture;
using MediatR;

namespace ExamShield.Api.Endpoints;

public static class PublicEndpoints
{
    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/public/verify", PublicVerifyAsync)
            .WithName("PublicVerify")
            .WithTags("Public")
            .AllowAnonymous()
            .Produces<PublicVerifyResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> PublicVerifyAsync(
        Guid? captureId, ISender sender, CancellationToken ct)
    {
        if (captureId is null)
            return Results.BadRequest("captureId query parameter is required.");

        var result = await sender.Send(new PublicVerifyCaptureQuery(captureId.Value), ct);

        return Results.Ok(new PublicVerifyResponse(
            result.CaptureId, result.IsValid, result.HashValid,
            result.SignatureValid, result.IsUploaded, result.CapturedAt));
    }
}
