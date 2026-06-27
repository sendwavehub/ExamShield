namespace ExamShield.Api.Contracts;

public sealed record StudentResultItemResponse(
    Guid ScoreId, Guid CaptureId, Guid ExamId, string ExamName,
    int CorrectAnswers, int TotalQuestions, double Percentage,
    DateTimeOffset ScoredAt, string HashHex, bool IsVerified);

public sealed record StudentResultsResponse(
    Guid StudentId,
    IReadOnlyList<StudentResultItemResponse> Results);
