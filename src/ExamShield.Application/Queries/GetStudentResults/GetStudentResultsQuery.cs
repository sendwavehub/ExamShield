using MediatR;

namespace ExamShield.Application.Queries.GetStudentResults;

public sealed record StudentResultItem(
    Guid ScoreId, Guid CaptureId, Guid ExamId,
    string ExamName, int CorrectAnswers, int TotalQuestions,
    double Percentage, DateTimeOffset ScoredAt,
    string HashHex, bool IsVerified);

public sealed record GetStudentResultsResult(Guid StudentId, IReadOnlyList<StudentResultItem> Results);

public sealed record GetStudentResultsQuery(Guid StudentId) : IRequest<GetStudentResultsResult>;
