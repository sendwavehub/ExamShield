using System.Collections.Concurrent;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.IntegrationTests.Fakes;

public sealed class InMemoryOcrResultRepository : IOcrResultRepository
{
    private readonly ConcurrentDictionary<Guid, OcrResult> _store = new();

    public Task AddAsync(OcrResult result, CancellationToken ct = default)
    {
        _store[result.Id.Value] = result;
        return Task.CompletedTask;
    }

    public Task<OcrResult?> GetByCaptureIdAsync(CaptureId captureId, CancellationToken ct = default) =>
        Task.FromResult(_store.Values.FirstOrDefault(r => r.CaptureId == captureId));
}
