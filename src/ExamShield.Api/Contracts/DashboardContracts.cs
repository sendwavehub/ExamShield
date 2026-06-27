namespace ExamShield.Api.Contracts;

public sealed record DashboardStatsResponse(
    int TotalCaptures,
    int PendingReview,
    int VerifiedToday,
    int ActiveAlerts
);
