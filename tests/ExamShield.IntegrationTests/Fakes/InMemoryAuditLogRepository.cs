using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.IntegrationTests.Fakes;

public sealed class InMemoryAuditLogRepository : IAuditLogRepository
{
    private readonly List<AuditLog> _entries = new();

    public Task AppendAsync(AuditLog entry, CancellationToken ct = default)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<AuditLog> Entries, int TotalCount)> QueryAsync(
        CaptureId? captureId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _entries.AsEnumerable();

        if (captureId is not null)
            query = query.Where(e => e.CaptureId == captureId);

        var total = query.Count();
        var entries = query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult<(IReadOnlyList<AuditLog>, int)>((entries, total));
    }
}
