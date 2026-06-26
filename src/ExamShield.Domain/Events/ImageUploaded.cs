using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Events;

public sealed record ImageUploaded(CaptureId CaptureId, string StorageKey) : DomainEvent;
