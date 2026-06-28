using System.Collections.Concurrent;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;

namespace ExamShield.Infrastructure.Persistence.Repositories;

/// <summary>
/// Thread-safe in-memory store for password reset tokens.
/// Replace with an EF Core implementation backed by a durable table
/// when persistence across restarts is required.
/// </summary>
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
