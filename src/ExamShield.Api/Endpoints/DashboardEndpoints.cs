using ExamShield.Api.Contracts;
using ExamShield.Application.Queries.GetDashboardStats;
using MediatR;

namespace ExamShield.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dashboard/stats", GetStatsAsync)
            .WithName("GetDashboardStats")
            .WithTags("Dashboard")
            .RequireAuthorization("Administrator")
            .Produces<DashboardStatsResponse>();

        return app;
    }

    private static async Task<IResult> GetStatsAsync(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetDashboardStatsQuery(), ct);
        return Results.Ok(new DashboardStatsResponse(
            result.TotalCaptures, result.PendingReview, result.VerifiedToday, result.ActiveAlerts));
    }
}
