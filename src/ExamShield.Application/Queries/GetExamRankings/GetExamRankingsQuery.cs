using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Queries.GetExamRankings;

public sealed record RankingEntry(
    int Rank, Guid StudentId, int CorrectAnswers, int TotalQuestions, double Percentage);

public sealed record GetExamRankingsResult(IReadOnlyList<RankingEntry> Rankings);

public sealed record GetExamRankingsQuery(Guid ExamId) : IRequest<GetExamRankingsResult>;

public sealed class GetExamRankingsQueryHandler(IScoreRepository scores)
    : IRequestHandler<GetExamRankingsQuery, GetExamRankingsResult>
{
    public async Task<GetExamRankingsResult> Handle(GetExamRankingsQuery request, CancellationToken ct)
    {
        var all = await scores.GetByExamIdAsync(new ExamId(request.ExamId), ct);

        var ordered = all
            .OrderByDescending(s => s.Percentage)
            .ThenByDescending(s => s.CorrectAnswers)
            .ThenBy(s => s.ScoredAt)
            .ThenBy(s => s.StudentId.Value)
            .ToList();

        var rankings = new List<RankingEntry>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var s    = ordered[i];
            var rank = i == 0 ? 1 : (s.Percentage == ordered[i - 1].Percentage ? rankings[i - 1].Rank : i + 1);
            rankings.Add(new RankingEntry(rank, s.StudentId.Value, s.CorrectAnswers, s.TotalQuestions, s.Percentage));
        }

        return new GetExamRankingsResult(rankings);
    }
}
