namespace ExamShield.Domain.Exceptions;

public sealed class WatermarkTamperedException(Guid captureId)
    : Exception($"Watermark validation failed for capture '{captureId}' — tampering detected.");
