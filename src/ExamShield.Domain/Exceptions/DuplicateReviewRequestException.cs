namespace ExamShield.Domain.Exceptions;

public sealed class DuplicateReviewRequestException(Guid captureId)
    : Exception($"A pending review request already exists for capture {captureId}.");
