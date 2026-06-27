namespace ExamShield.Domain.Exceptions;

public sealed class DuplicateCaptureException(Guid examId, Guid studentId, int pageNumber)
    : Exception($"Student {studentId} already has an active capture for exam {examId} page {pageNumber}.");
