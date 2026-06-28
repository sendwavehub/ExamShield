using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ExamShield.Infrastructure.Persistence.Repositories;

public sealed class ExamCandidateRepository(ExamShieldDbContext db) : IExamCandidateRepository
{
    public async Task AddAsync(ExamCandidate candidate, CancellationToken ct = default)
    {
        db.ExamCandidates.Add(candidate);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ExamCandidate>> ListByExamIdAsync(ExamId examId, CancellationToken ct = default) =>
        await db.ExamCandidates
            .AsNoTracking()
            .Where(c => c.ExamId == examId)
            .ToListAsync(ct);

    public Task<bool> ExistsAsync(ExamId examId, StudentId studentId, CancellationToken ct = default) =>
        db.ExamCandidates.AnyAsync(c => c.ExamId == examId && c.StudentId == studentId, ct);

    public Task<int> CountByExamIdAsync(ExamId examId, CancellationToken ct = default) =>
        db.ExamCandidates.CountAsync(c => c.ExamId == examId, ct);

    public async Task RemoveAsync(ExamId examId, StudentId studentId, CancellationToken ct = default)
    {
        var candidate = await db.ExamCandidates
            .FirstOrDefaultAsync(c => c.ExamId == examId && c.StudentId == studentId, ct);
        if (candidate is not null)
        {
            db.ExamCandidates.Remove(candidate);
            await db.SaveChangesAsync(ct);
        }
    }
}
