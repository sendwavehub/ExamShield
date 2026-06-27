using ExamShield.Domain.Interfaces;

namespace ExamShield.IntegrationTests.Fakes;

public sealed class InMemoryTotpUsedCodeCache : ITotpUsedCodeCache
{
    private readonly HashSet<string> _used = [];

    public Task<bool> IsUsedAsync(string userId, string code, CancellationToken ct = default) =>
        Task.FromResult(_used.Contains($"{userId}:{code}"));

    public Task MarkUsedAsync(string userId, string code, CancellationToken ct = default)
    {
        _used.Add($"{userId}:{code}");
        return Task.CompletedTask;
    }
}
