using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public UserId UserId { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsExpired && !IsRevoked;

    private RefreshToken() { }

    public static RefreshToken Create(UserId userId, string tokenHash, int expiryDays)
    {
        var now = DateTimeOffset.UtcNow;
        return new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddDays(expiryDays),
        };
    }

    public void Revoke() => RevokedAt = DateTimeOffset.UtcNow;
}
