using MediatR;

namespace ExamShield.Application.Commands.CreateExam;

public sealed record CreateExamResult(Guid ExamId, string Name, string? Description, int TotalQuestions, string Status, DateTimeOffset CreatedAt);

public sealed record CreateExamCommand(string Name, string? Description, int TotalQuestions)
    : IRequest<CreateExamResult>;
