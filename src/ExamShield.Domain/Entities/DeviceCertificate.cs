using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Entities;

public sealed class DeviceCertificate
{
    public Guid Id { get; private set; }
    public DeviceId DeviceId { get; private set; } = null!;
    public string PublicKeyPem { get; private set; } = null!;
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevocationReason { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
    public bool IsValid   => !IsRevoked && !IsExpired;

    private DeviceCertificate() { } // EF Core

    public static DeviceCertificate Issue(
        DeviceId deviceId,
        string publicKeyPem,
        int validDays,
        DateTimeOffset? issuedAt = null)
    {
        ArgumentNullException.ThrowIfNull(deviceId, nameof(deviceId));
        ArgumentException.ThrowIfNullOrWhiteSpace(publicKeyPem, nameof(publicKeyPem));
        if (validDays <= 0) throw new ArgumentOutOfRangeException(nameof(validDays));

        var now = issuedAt ?? DateTimeOffset.UtcNow;
        return new DeviceCertificate
        {
            Id          = Guid.NewGuid(),
            DeviceId    = deviceId,
            PublicKeyPem = publicKeyPem.Trim(),
            IssuedAt    = now,
            ExpiresAt   = now.AddDays(validDays),
        };
    }

    public void Revoke(string reason)
    {
        if (IsRevoked)
            throw new InvalidOperationException("Certificate is already revoked.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        RevokedAt = DateTimeOffset.UtcNow;
        RevocationReason = reason.Trim();
    }
}
