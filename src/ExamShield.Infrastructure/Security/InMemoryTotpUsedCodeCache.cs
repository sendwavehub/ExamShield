using System.Collections.Concurrent;
using ExamShield.Domain.Interfaces;

namespace ExamShield.Infrastructure.Security;

// Production deployments should replace this with a Redis-backed implementation
// with a TTL of ~90 seconds (one TOTP window either side of the current window).
public sealed class InMemoryTotpUsedCodeCache : ITotpUsedCodeCache
{
    private readonly ConcurrentDictionary<string, byte> _used = new();

    public Task<bool> IsUsedAsync(string userId, string code, CancellationToken ct = default) =>
        Task.FromResult(_used.ContainsKey($"{userId}:{code}"));

    public Task MarkUsedAsync(string userId, string code, CancellationToken ct = default)
    {
        _used.TryAdd($"{userId}:{code}", 0);
        return Task.CompletedTask;
    }
}
