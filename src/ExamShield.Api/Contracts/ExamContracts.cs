namespace ExamShield.Api.Contracts;

public sealed record CreateExamRequest(string Name, string? Description, int TotalQuestions);

public sealed record ExamResponse(
    Guid ExamId, string Name, string? Description,
    string Status, int TotalQuestions, DateTimeOffset CreatedAt);

public sealed record ExamListResponse(IReadOnlyList<ExamResponse> Exams);
