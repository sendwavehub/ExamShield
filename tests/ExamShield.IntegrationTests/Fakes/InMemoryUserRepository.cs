using System.Collections.Concurrent;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.IntegrationTests.Fakes;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _byEmail = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryUserRepository(IEnumerable<User>? seed = null)
    {
        foreach (var user in seed ?? [])
            _byEmail[user.Email.Value] = user;
    }

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        if (!_byEmail.TryAdd(user.Email.Value, user))
            throw new InvalidOperationException($"User '{user.Email.Value}' already exists.");
        return Task.CompletedTask;
    }

    public Task<User?> FindByEmailAsync(Email email, CancellationToken ct = default) =>
        Task.FromResult(_byEmail.TryGetValue(email.Value, out var user) ? user : null);
}
