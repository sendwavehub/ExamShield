using MediatR;

namespace ExamShield.Application.Queries.GetDashboardStats;

public sealed record GetDashboardStatsQuery : IRequest<GetDashboardStatsResult>;

public sealed record GetDashboardStatsResult(
    int TotalCaptures,
    int PendingReview,
    int VerifiedToday,
    int ActiveAlerts
);
