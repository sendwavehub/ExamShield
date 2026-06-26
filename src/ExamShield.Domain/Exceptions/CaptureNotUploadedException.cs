namespace ExamShield.Domain.Exceptions;

public sealed class CaptureNotUploadedException : Exception
{
    public CaptureNotUploadedException(Guid captureId)
        : base($"Capture {captureId} has not been uploaded yet.")
    { }
}
