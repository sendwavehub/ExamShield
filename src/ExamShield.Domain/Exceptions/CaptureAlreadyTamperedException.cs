namespace ExamShield.Domain.Exceptions;

public sealed class CaptureAlreadyTamperedException(Guid captureId)
    : Exception($"Capture {captureId} is already flagged as tampered.");
