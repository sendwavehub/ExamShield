using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Events;

public sealed record CaptureCreated(
    CaptureId CaptureId,
    ExamId ExamId,
    StudentId StudentId
) : DomainEvent;
