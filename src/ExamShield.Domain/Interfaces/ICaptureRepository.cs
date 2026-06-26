using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Interfaces;

public interface ICaptureRepository
{
    Task AddAsync(Capture capture, CancellationToken ct = default);
    Task UpdateAsync(Capture capture, CancellationToken ct = default);
    Task<Capture?> GetByIdAsync(CaptureId id, CancellationToken ct = default);
}
