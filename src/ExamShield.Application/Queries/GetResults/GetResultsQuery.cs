using MediatR;

namespace ExamShield.Application.Queries.GetResults;

public sealed record GetResultsQuery : IRequest<GetResultsResult>;

public sealed record ScoreDto(
    Guid ScoreId, Guid CaptureId, Guid ExamId, Guid StudentId,
    int CorrectAnswers, int TotalQuestions, double Percentage, DateTimeOffset ScoredAt);

public sealed record GetResultsResult(IReadOnlyList<ScoreDto> Results);
