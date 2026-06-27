using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ExamShield.Infrastructure.Persistence.Repositories;

public sealed class ExamRepository(ExamShieldDbContext context) : IExamRepository
{
    public async Task AddAsync(Exam exam, CancellationToken ct = default)
    {
        await context.Exams.AddAsync(exam, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Exam exam, CancellationToken ct = default)
    {
        context.Exams.Update(exam);
        await context.SaveChangesAsync(ct);
    }

    public Task<Exam?> GetByIdAsync(ExamId id, CancellationToken ct = default) =>
        context.Exams.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Exam>> ListAllAsync(CancellationToken ct = default) =>
        await context.Exams.ToListAsync(ct);
}
