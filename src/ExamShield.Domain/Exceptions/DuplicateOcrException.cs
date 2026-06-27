namespace ExamShield.Domain.Exceptions;

public sealed class DuplicateOcrException(Guid captureId)
    : Exception($"An OCR result already exists for capture {captureId}.");
