using System.Collections.Concurrent;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;

namespace ExamShield.Infrastructure.Storage;

public sealed class InMemoryImageStorage : IImageStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new();

    public Task<string> StoreAsync(Guid captureId, byte[] imageBytes, CancellationToken ct = default)
    {
        var key = $"captures/{captureId:N}";
        _store[key] = imageBytes.ToArray();
        return Task.FromResult(key);
    }

    public Task<byte[]> RetrieveAsync(string storageKey, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(storageKey, out var bytes))
            throw new ImageNotFoundException(storageKey);
        return Task.FromResult(bytes.ToArray());
    }
}
