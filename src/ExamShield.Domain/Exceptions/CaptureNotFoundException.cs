namespace ExamShield.Domain.Exceptions;

public sealed class CaptureNotFoundException : Exception
{
    public CaptureNotFoundException(Guid captureId)
        : base($"Capture '{captureId}' was not found.") { }
}
