using System.Collections.Concurrent;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;

namespace ExamShield.IntegrationTests.Fakes;

public sealed class InMemoryManualReviewRepository : IManualReviewRepository
{
    private readonly ConcurrentDictionary<Guid, ManualReview> _store = new();

    public Task AddAsync(ManualReview review, CancellationToken ct = default)
    {
        _store[review.Id.Value] = review;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ManualReview>> GetPendingAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ManualReview>>(
            _store.Values.Where(r => r.Status == ManualReviewStatus.Pending).ToList());
}
