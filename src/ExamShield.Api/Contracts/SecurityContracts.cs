namespace ExamShield.Api.Contracts;

public sealed record SecurityEventResponse(
    Guid Id,
    string EventType,
    string Severity,
    string Message,
    string? UserId,
    string? IpAddress,
    Guid? CaptureId,
    DateTimeOffset OccurredAt
);

public sealed record SecurityEventListResponse(IReadOnlyList<SecurityEventResponse> Events);

public sealed record LoginHistoryEntry(
    Guid Id,
    string EventType,
    string? UserId,
    string? IpAddress,
    DateTimeOffset OccurredAt
);

public sealed record LoginHistoryResponse(IReadOnlyList<LoginHistoryEntry> Events);
