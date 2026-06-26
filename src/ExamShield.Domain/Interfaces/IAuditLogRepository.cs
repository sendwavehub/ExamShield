using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Interfaces;

public interface IAuditLogRepository
{
    // Append-only: no UpdateAsync, no DeleteAsync
    Task AppendAsync(AuditLog entry, CancellationToken ct = default);

    Task<(IReadOnlyList<AuditLog> Entries, int TotalCount)> QueryAsync(
        CaptureId? captureId, int page, int pageSize, CancellationToken ct = default);
}
