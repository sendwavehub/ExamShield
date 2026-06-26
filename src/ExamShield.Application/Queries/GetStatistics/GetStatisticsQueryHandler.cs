using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Queries.GetStatistics;

public sealed class GetStatisticsQueryHandler : IRequestHandler<GetStatisticsQuery, GetStatisticsResult>
{
    private readonly IScoreRepository _scores;

    public GetStatisticsQueryHandler(IScoreRepository scores) => _scores = scores;

    public async Task<GetStatisticsResult> Handle(GetStatisticsQuery query, CancellationToken ct)
    {
        var scores = await _scores.GetAllAsync(ct);

        if (scores.Count == 0)
            return new GetStatisticsResult(0, 0.0, 0, 0);

        return new GetStatisticsResult(
            scores.Count,
            Math.Round(scores.Average(s => s.Percentage), 2),
            scores.Max(s => s.CorrectAnswers),
            scores.Min(s => s.CorrectAnswers));
    }
}
