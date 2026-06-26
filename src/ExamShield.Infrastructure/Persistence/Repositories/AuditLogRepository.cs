using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ExamShield.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly ExamShieldDbContext _context;

    public AuditLogRepository(ExamShieldDbContext context) => _context = context;

    public async Task AppendAsync(AuditLog entry, CancellationToken ct = default)
    {
        await _context.AuditLogs.AddAsync(entry, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<AuditLog> Entries, int TotalCount)> QueryAsync(
        CaptureId? captureId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (captureId is not null)
            query = query.Where(e => e.CaptureId == captureId);

        var total = await query.CountAsync(ct);

        var entries = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (entries, total);
    }
}
