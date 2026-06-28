using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ExamShield.Infrastructure.Persistence.Repositories;

public sealed class AnswerKeyRepository(ExamShieldDbContext db) : IAnswerKeyRepository
{
    public async Task<AnswerKey?> GetByExamIdAsync(ExamId examId, CancellationToken ct = default)
    {
        var entity = await db.ExamAnswerKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.ExamId == examId, ct);
        return entity?.ToValueObject();
    }

    public Task<ExamAnswerKey?> GetEntityByExamIdAsync(ExamId examId, CancellationToken ct = default) =>
        db.ExamAnswerKeys.FirstOrDefaultAsync(k => k.ExamId == examId, ct);

    public async Task SaveAsync(ExamAnswerKey key, CancellationToken ct = default)
    {
        var existing = await db.ExamAnswerKeys.FindAsync([key.ExamId], ct);
        if (existing is null)
            db.ExamAnswerKeys.Add(key);
        else
            db.Entry(existing).CurrentValues.SetValues(key);
        await db.SaveChangesAsync(ct);
    }
}
