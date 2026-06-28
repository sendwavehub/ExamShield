using System.Security.Cryptography;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Commands.Refresh;

public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    IJwtTokenService jwt,
    ISecurityEventRepository securityEvents,
    IAuditLogRepository auditLog)
    : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var hash   = HashToken(cmd.RefreshToken);
        var stored = await refreshTokens.FindByHashAsync(hash, ct);

        // Token reuse: revoked but not yet expired = stolen token being replayed
        if (stored is { IsRevoked: true, IsExpired: false })
        {
            await refreshTokens.RevokeAllForUserAsync(stored.UserId, ct);
            await securityEvents.AddAsync(
                SecurityEvent.Create(SecurityEventType.TokenTheftDetected, SecuritySeverity.Critical,
                    message: $"Refresh token reuse detected for user '{stored.UserId.Value}'.",
                    userId: stored.UserId.Value.ToString()),
                ct);
            throw new InvalidCredentialsException();
        }

        if (stored is null || !stored.IsActive)
            throw new InvalidCredentialsException();

        var user = await users.GetByIdAsync(stored.UserId, ct)
            ?? throw new InvalidCredentialsException();

        if (!user.IsActive || user.IsLockedOut)
            throw new InvalidCredentialsException();

        stored.Revoke();
        await refreshTokens.SaveAsync(stored, ct);

        var rawToken = ExamShield.Application.Commands.Login.LoginCommandHandler.GenerateRawToken();
        var newToken = RefreshToken.Create(user.Id, HashToken(rawToken), expiryDays: 7);
        await refreshTokens.AddAsync(newToken, ct);
        await auditLog.AppendAsync(AuditLog.Record(AuditAction.TokenRefreshed), ct);

        return new RefreshTokenResult(jwt.Generate(user), rawToken, user.Role.ToString());
    }

    private static string HashToken(string raw) =>
        Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));
}
