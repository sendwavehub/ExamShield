using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Queries.GetDashboardStats;

public sealed class GetDashboardStatsQueryHandler(
    ICaptureRepository captures,
    IManualReviewRepository reviews,
    ISecurityEventRepository securityEvents)
    : IRequestHandler<GetDashboardStatsQuery, GetDashboardStatsResult>
{
    public async Task<GetDashboardStatsResult> Handle(GetDashboardStatsQuery request, CancellationToken ct)
    {
        var todayStart = DateTimeOffset.UtcNow.Date;

        var totalTask    = captures.CountAsync(ct);
        var verifiedTask = captures.CountVerifiedSinceAsync(todayStart, ct);
        var reviewsTask  = reviews.GetPendingAsync(ct);
        var alertsTask   = securityEvents.ListRecentAsync(1000, ct);

        await Task.WhenAll(totalTask, verifiedTask, reviewsTask, alertsTask);

        var activeAlerts = (await alertsTask)
            .Count(e => e.Severity == SecuritySeverity.Critical
                     && e.OccurredAt >= DateTimeOffset.UtcNow.AddHours(-24));

        return new GetDashboardStatsResult(
            await totalTask,
            (await reviewsTask).Count,
            await verifiedTask,
            activeAlerts
        );
    }
}
