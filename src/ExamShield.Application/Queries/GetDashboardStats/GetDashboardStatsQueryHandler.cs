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
        // Sequential — DbContext is not thread-safe; Task.WhenAll causes concurrency errors.
        var todayStart   = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);
        var total        = await captures.CountAsync(ct);
        var verified     = await captures.CountVerifiedSinceAsync(todayStart, ct);
        var pending      = await reviews.GetPendingAsync(ct);
        var recentAlerts = await securityEvents.ListRecentAsync(1000, ct);

        var activeAlerts = recentAlerts
            .Count(e => e.Severity == SecuritySeverity.Critical
                     && e.OccurredAt >= DateTimeOffset.UtcNow.AddHours(-24));

        return new GetDashboardStatsResult(total, pending.Count, verified, activeAlerts);
    }
}
