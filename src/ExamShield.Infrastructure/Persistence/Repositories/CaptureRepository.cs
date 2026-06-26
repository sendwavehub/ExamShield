using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ExamShield.Infrastructure.Persistence.Repositories;

public sealed class CaptureRepository : ICaptureRepository
{
    private readonly ExamShieldDbContext _context;

    public CaptureRepository(ExamShieldDbContext context) => _context = context;

    public async Task AddAsync(Capture capture, CancellationToken ct = default)
    {
        await _context.Captures.AddAsync(capture, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Capture capture, CancellationToken ct = default)
    {
        _context.Captures.Update(capture);
        await _context.SaveChangesAsync(ct);
    }

    public Task<Capture?> GetByIdAsync(CaptureId id, CancellationToken ct = default) =>
        _context.Captures.FirstOrDefaultAsync(c => c.Id == id, ct);
}
