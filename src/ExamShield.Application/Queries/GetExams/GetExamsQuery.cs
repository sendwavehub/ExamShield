using MediatR;

namespace ExamShield.Application.Queries.GetExams;

public sealed record ExamDto(
    Guid ExamId, string Name, string? Description,
    string Status, int TotalQuestions, DateTimeOffset CreatedAt);

public sealed record GetExamsResult(IReadOnlyList<ExamDto> Exams);

public sealed record GetExamsQuery : IRequest<GetExamsResult>;
