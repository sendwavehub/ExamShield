using System.Collections.Concurrent;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;

namespace ExamShield.IntegrationTests.Fakes;

public sealed class InMemoryPasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly ConcurrentDictionary<string, PasswordResetToken> _store = new();

    public Task AddAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        _store[token.Token] = token;
        return Task.CompletedTask;
    }

    public Task<PasswordResetToken?> FindAsync(string token, CancellationToken ct = default)
    {
        _store.TryGetValue(token, out var found);
        return Task.FromResult(found);
    }

    public Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        _store[token.Token] = token;
        return Task.CompletedTask;
    }
}
