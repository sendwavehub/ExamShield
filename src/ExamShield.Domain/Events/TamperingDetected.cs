using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Events;

public sealed record TamperingDetected(
    CaptureId CaptureId,
    Hash ExpectedHash,
    Hash ActualHash
) : DomainEvent;
