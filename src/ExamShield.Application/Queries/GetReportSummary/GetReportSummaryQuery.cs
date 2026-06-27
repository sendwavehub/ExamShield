using MediatR;

namespace ExamShield.Application.Queries.GetReportSummary;

public sealed record CaptureStats(int Total, int Created, int Uploaded, int Verified, int Tampered);
public sealed record OcrStats(int TotalProcessed, double AverageConfidence);
public sealed record ScoreStats(int TotalScored, double AveragePercentage, double HighestPercentage, double LowestPercentage);
public sealed record SecurityStats(int TotalEvents, int CriticalEvents);

public sealed record GetReportSummaryResult(
    DateTimeOffset GeneratedAt,
    CaptureStats Captures,
    OcrStats Ocr,
    ScoreStats Scores,
    SecurityStats Security);

public sealed record GetReportSummaryQuery : IRequest<GetReportSummaryResult>;
