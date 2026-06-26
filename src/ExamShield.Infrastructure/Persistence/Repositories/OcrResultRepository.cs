using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ExamShield.Infrastructure.Persistence.Repositories;

public sealed class OcrResultRepository : IOcrResultRepository
{
    private readonly ExamShieldDbContext _context;

    public OcrResultRepository(ExamShieldDbContext context) => _context = context;

    public async Task AddAsync(OcrResult result, CancellationToken ct = default)
    {
        await _context.OcrResults.AddAsync(result, ct);
        await _context.SaveChangesAsync(ct);
    }

    public Task<OcrResult?> GetByCaptureIdAsync(CaptureId captureId, CancellationToken ct = default) =>
        _context.OcrResults
            .FirstOrDefaultAsync(r => r.CaptureId == captureId, ct);
}
