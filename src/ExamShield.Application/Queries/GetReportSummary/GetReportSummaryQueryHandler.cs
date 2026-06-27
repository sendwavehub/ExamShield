using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Queries.GetReportSummary;

public sealed class GetReportSummaryQueryHandler(
    ICaptureRepository captures,
    IOcrResultRepository ocrResults,
    IScoreRepository scores,
    ISecurityEventRepository securityEvents)
    : IRequestHandler<GetReportSummaryQuery, GetReportSummaryResult>
{
    public async Task<GetReportSummaryResult> Handle(GetReportSummaryQuery request, CancellationToken ct)
    {
        var allCaptures = await captures.ListAllAsync(ct);
        var captureStats = new CaptureStats(
            Total:    allCaptures.Count,
            Created:  allCaptures.Count(c => c.Status == CaptureStatus.Created),
            Uploaded: allCaptures.Count(c => c.Status == CaptureStatus.Uploaded),
            Verified: allCaptures.Count(c => c.Status == CaptureStatus.Verified),
            Tampered: allCaptures.Count(c => c.Status == CaptureStatus.Tampered));

        var completed = await ocrResults.ListCompletedAsync(ct);
        var avgConf = completed.Count > 0
            ? completed.Average(o => o.OverallConfidence.Value)
            : 0.0;
        var ocrStats = new OcrStats(TotalProcessed: completed.Count, AverageConfidence: avgConf);

        var allScores = await scores.GetAllAsync(ct);
        ScoreStats scoreStats;
        if (allScores.Count > 0)
        {
            scoreStats = new ScoreStats(
                TotalScored:       allScores.Count,
                AveragePercentage: allScores.Average(s => s.Percentage),
                HighestPercentage: allScores.Max(s => s.Percentage),
                LowestPercentage:  allScores.Min(s => s.Percentage));
        }
        else
        {
            scoreStats = new ScoreStats(0, 0, 0, 0);
        }

        var totalEvents    = await securityEvents.CountAllAsync(ct);
        var criticalEvents = await securityEvents.CountBySeverityAsync(SecuritySeverity.Critical, ct);
        var securityStats  = new SecurityStats(totalEvents, criticalEvents);

        return new GetReportSummaryResult(DateTimeOffset.UtcNow, captureStats, ocrStats, scoreStats, securityStats);
    }
}
