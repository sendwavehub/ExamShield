using System.Collections.Concurrent;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.IntegrationTests.Fakes;

public sealed class InMemoryCaptureRepository : ICaptureRepository
{
    private readonly ConcurrentDictionary<CaptureId, Capture> _store = new();

    public Task AddAsync(Capture capture, CancellationToken ct = default)
    {
        _store[capture.Id] = capture;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Capture capture, CancellationToken ct = default)
    {
        _store[capture.Id] = capture;
        return Task.CompletedTask;
    }

    public Task<Capture?> GetByIdAsync(CaptureId id, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(id, out var capture) ? capture : null);
}
