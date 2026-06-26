namespace ExamShield.Domain.Exceptions;

public sealed class DuplicateUploadException : Exception
{
    public DuplicateUploadException(Guid captureId)
        : base($"Capture {captureId} has already been uploaded.")
    { }
}
