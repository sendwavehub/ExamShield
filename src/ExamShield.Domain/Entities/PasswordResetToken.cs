namespace ExamShield.Domain.Entities;

public sealed class PasswordResetToken
{
    public string Token { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
    public bool IsUsed    => UsedAt.HasValue;
    public bool IsValid   => !IsExpired && !IsUsed;

    private PasswordResetToken() { }

    public static PasswordResetToken Create(string email, DateTimeOffset? expiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));
        return new PasswordResetToken
        {
            Token     = Guid.NewGuid().ToString("N"),
            Email     = email.ToLowerInvariant(),
            ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddHours(1),
        };
    }

    public void MarkUsed() => UsedAt = DateTimeOffset.UtcNow;
}
