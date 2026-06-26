namespace ExamShield.Domain.Exceptions;

public sealed class OcrResultNotFoundException : Exception
{
    public OcrResultNotFoundException(Guid captureId)
        : base($"OCR result for capture '{captureId}' was not found.") { }
}
