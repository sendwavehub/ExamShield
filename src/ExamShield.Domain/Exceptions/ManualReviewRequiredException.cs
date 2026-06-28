namespace ExamShield.Domain.Exceptions;

public sealed class ManualReviewRequiredException(Guid captureId)
    : Exception($"Capture '{captureId}' requires manual review before scoring.");
