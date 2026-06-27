using ExamShield.Api.Contracts;
using ExamShield.Application.Queries.GetReportSummary;
using MediatR;

namespace ExamShield.Api.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/summary", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetReportSummaryQuery(), ct);
            return Results.Ok(new ReportSummaryResponse(
                result.GeneratedAt,
                new CaptureStatsResponse(
                    result.Captures.Total, result.Captures.Created,
                    result.Captures.Uploaded, result.Captures.Verified, result.Captures.Tampered),
                new OcrStatsResponse(result.Ocr.TotalProcessed, result.Ocr.AverageConfidence),
                new ScoreStatsResponse(
                    result.Scores.TotalScored, result.Scores.AveragePercentage,
                    result.Scores.HighestPercentage, result.Scores.LowestPercentage),
                new SecurityStatsResponse(result.Security.TotalEvents, result.Security.CriticalEvents)));
        })
        .WithName("GetReportSummary")
        .WithTags("Reports")
        .RequireAuthorization("Operator")
        .Produces<ReportSummaryResponse>();

        return app;
    }
}
