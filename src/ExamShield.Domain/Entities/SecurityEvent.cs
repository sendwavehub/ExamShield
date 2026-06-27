using ExamShield.Domain.Enums;

namespace ExamShield.Domain.Entities;

public sealed class SecurityEvent
{
    public Guid Id { get; private set; }
    public SecurityEventType EventType { get; private set; }
    public SecuritySeverity Severity { get; private set; }
    public string Message { get; private set; } = null!;
    public string? UserId { get; private set; }
    public string? IpAddress { get; private set; }
    public Guid? CaptureId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private SecurityEvent() { } // EF Core

    public static SecurityEvent Create(
        SecurityEventType eventType,
        SecuritySeverity severity,
        string message,
        string? userId = null,
        string? ipAddress = null,
        Guid? captureId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message, nameof(message));
        return new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Severity = severity,
            Message = message,
            UserId = userId,
            IpAddress = ipAddress,
            CaptureId = captureId,
            OccurredAt = DateTimeOffset.UtcNow,
        };
    }
}
