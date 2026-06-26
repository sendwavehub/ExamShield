namespace ExamShield.Domain.Events;

public abstract record DomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
