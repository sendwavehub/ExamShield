using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Queries.GetResults;

public sealed class GetResultsQueryHandler : IRequestHandler<GetResultsQuery, GetResultsResult>
{
    private readonly IScoreRepository _scores;

    public GetResultsQueryHandler(IScoreRepository scores) => _scores = scores;

    public async Task<GetResultsResult> Handle(GetResultsQuery query, CancellationToken ct)
    {
        var scores = await _scores.GetPublishedAsync(ct);
        var dtos = scores
            .Select(s => new ScoreDto(
                s.Id.Value, s.CaptureId.Value, s.ExamId.Value, s.StudentId.Value,
                s.CorrectAnswers, s.TotalQuestions, s.Percentage, s.ScoredAt))
            .ToList();
        return new GetResultsResult(dtos);
    }
}
