namespace ExamShield.Domain.Exceptions;

public sealed class ExamExpiredException(Guid examId, DateTimeOffset endsAt)
    : Exception($"Exam {examId} ended at {endsAt:O} and no longer accepts captures.");
