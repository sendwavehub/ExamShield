namespace ExamShield.Domain.Exceptions;

public sealed class InvalidSignatureException : Exception
{
    public InvalidSignatureException(Guid deviceId)
        : base($"Signature verification failed for device {deviceId}.")
    { }
}
