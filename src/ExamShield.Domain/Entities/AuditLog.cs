using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Entities;

public sealed class AuditLog
{
    public AuditLogId Id { get; private set; } = null!;
    public AuditAction Action { get; private set; }
    public CaptureId? CaptureId { get; private set; }
    public string UserId { get; private set; } = null!;
    public string IpAddress { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }
    public string? Reason { get; private set; }
    public string PreviousHash { get; private set; } = string.Empty;
    public string ContentHash { get; private set; } = string.Empty;

    private AuditLog() { } // EF Core

    public static AuditLog Record(
        AuditAction action,
        CaptureId? captureId = null,
        string userId = "system",
        string ipAddress = "unknown",
        string? reason = null) =>
        new()
        {
            Id = AuditLogId.New(),
            Action = action,
            CaptureId = captureId,
            UserId = userId,
            IpAddress = ipAddress,
            OccurredAt = DateTimeOffset.UtcNow,
            Reason = reason
        };

    // Called by the repository before persisting to link this entry into the chain.
    public void SetChainHashes(string previousHash, string contentHash)
    {
        PreviousHash = previousHash;
        ContentHash = contentHash;
    }
}
