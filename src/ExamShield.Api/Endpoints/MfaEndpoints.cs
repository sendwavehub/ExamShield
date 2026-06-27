using System.Security.Claims;
using ExamShield.Api.Contracts;
using ExamShield.Application.Commands.MfaDisable;
using ExamShield.Application.Commands.MfaSetup;
using ExamShield.Application.Commands.MfaVerify;
using ExamShield.Application.Queries.GetMfaStatus;
using MediatR;

namespace ExamShield.Api.Endpoints;

public static class MfaEndpoints
{
    public static IEndpointRouteBuilder MapMfaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth/mfa").WithTags("MFA").RequireAuthorization();

        group.MapGet("/status", GetStatusAsync)
            .WithName("GetMfaStatus")
            .Produces<MfaStatusResponse>();

        group.MapPost("/setup", SetupAsync)
            .WithName("SetupMfa")
            .Produces<MfaSetupResponse>();

        group.MapPost("/verify", VerifyAsync)
            .WithName("VerifyMfa")
            .Produces<MfaStatusResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapDelete("/", DisableAsync)
            .WithName("DisableMfa")
            .Produces<MfaStatusResponse>();

        return app;
    }

    private static async Task<IResult> GetStatusAsync(
        ClaimsPrincipal user, ISender sender, CancellationToken ct)
    {
        var userId = ExtractUserId(user);
        var result = await sender.Send(new GetMfaStatusQuery(userId), ct);
        return Results.Ok(new MfaStatusResponse(result.MfaEnabled));
    }

    private static async Task<IResult> SetupAsync(
        ClaimsPrincipal user, ISender sender, CancellationToken ct)
    {
        var userId = ExtractUserId(user);
        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email") ?? "";
        var result = await sender.Send(new MfaSetupCommand(userId, email), ct);
        return Results.Ok(new MfaSetupResponse(result.Secret, result.QrUri));
    }

    private static async Task<IResult> VerifyAsync(
        MfaVerifyRequest req, ClaimsPrincipal user, ISender sender, CancellationToken ct)
    {
        var userId = ExtractUserId(user);
        try
        {
            var result = await sender.Send(new MfaVerifyCommand(userId, req.Code), ct);
            return Results.Ok(new MfaStatusResponse(result.MfaEnabled));
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
    }

    private static async Task<IResult> DisableAsync(
        ClaimsPrincipal user, ISender sender, CancellationToken ct)
    {
        var userId = ExtractUserId(user);
        var result = await sender.Send(new MfaDisableCommand(userId), ct);
        return Results.Ok(new MfaStatusResponse(result.MfaEnabled));
    }

    private static Guid ExtractUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Missing user ID claim.");
        return Guid.Parse(raw);
    }
}
