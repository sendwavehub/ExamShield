using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Exceptions;

public sealed class HashMismatchException : Exception
{
    public HashMismatchException(Guid captureId, Hash expected, Hash actual)
        : base($"Hash mismatch for capture {captureId}: expected {expected.Hex[..8]}…, got {actual.Hex[..8]}…")
    { }
}
