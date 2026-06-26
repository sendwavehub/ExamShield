using ExamShield.Domain.Entities;

namespace ExamShield.Domain.Interfaces;

public interface IManualReviewRepository
{
    Task AddAsync(ManualReview review, CancellationToken ct = default);
    Task<IReadOnlyList<ManualReview>> GetPendingAsync(CancellationToken ct = default);
}
