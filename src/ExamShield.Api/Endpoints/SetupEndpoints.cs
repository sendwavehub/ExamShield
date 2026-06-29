using ExamShield.Api.Contracts;
using ExamShield.Application.Commands.CompleteSetup;
using ExamShield.Application.Queries.GetSetupStatus;
using MediatR;

namespace ExamShield.Api.Endpoints;

public static class SetupEndpoints
{
    public static IEndpointRouteBuilder MapSetupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/setup").WithTags("Setup").AllowAnonymous();

        group.MapGet("/status", GetSetupStatusAsync)
            .WithName("GetSetupStatus")
            .Produces<SetupStatusResponse>()
            .WithSummary("Returns first-run flag and service health checks. Always public.");

        group.MapPost("/complete", CompleteSetupAsync)
            .WithName("CompleteSetup")
            .Produces<CompleteSetupResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Creates the first Super Administrator and optionally loads demo data. Locks after first use.");

        return app;
    }

    private static async Task<IResult> GetSetupStatusAsync(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetSetupStatusQuery(), ct);
        return Results.Ok(new SetupStatusResponse(result.IsFirstRun, result.Version, result.Checks));
    }

    private static async Task<IResult> CompleteSetupAsync(
        CompleteSetupRequest request, ISender sender, CancellationToken ct)
    {
        try
        {
            var result = await sender.Send(
                new CompleteSetupCommand(
                    request.AdminEmail,
                    request.AdminDisplayName,
                    request.AdminPassword,
                    request.SeedDemoData),
                ct);
            return Results.Created("/setup/status",
                new CompleteSetupResponse(result.AdminUserId, "Setup complete. Please log in."));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { title = ex.Message, status = 409 });
        }
    }
}
