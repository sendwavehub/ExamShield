namespace ExamShield.Api.Contracts;

public sealed record AuditLogResponse(
    IReadOnlyList<AuditLogEntryResponse> Entries,
    int TotalCount
);

public sealed record AuditLogEntryResponse(
    Guid Id,
    string Action,
    Guid? CaptureId,
    string UserId,
    string IpAddress,
    DateTimeOffset OccurredAt,
    string? Reason,
    string ContentHash
);

public sealed record VerifyAuditChainResponse(
    bool IsValid,
    int EntryCount,
    int? FirstBrokenIndex
);
