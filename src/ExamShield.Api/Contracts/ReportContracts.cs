namespace ExamShield.Api.Contracts;

public sealed record CaptureStatsResponse(int Total, int Created, int Uploaded, int Verified, int Tampered);
public sealed record OcrStatsResponse(int TotalProcessed, double AverageConfidence);
public sealed record ScoreStatsResponse(int TotalScored, double AveragePercentage, double HighestPercentage, double LowestPercentage);
public sealed record SecurityStatsResponse(int TotalEvents, int CriticalEvents);

public sealed record ReportSummaryResponse(
    DateTimeOffset GeneratedAt,
    CaptureStatsResponse Captures,
    OcrStatsResponse Ocr,
    ScoreStatsResponse Scores,
    SecurityStatsResponse Security);
