using System.Security.Cryptography;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Commands.Logout;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokens,
    IAuditLogRepository auditLog)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand cmd, CancellationToken ct)
    {
        var hash = HashToken(cmd.RefreshToken);
        var stored = await refreshTokens.FindByHashAsync(hash, ct);
        if (stored is not null && stored.IsActive)
        {
            stored.Revoke();
            await refreshTokens.SaveAsync(stored, ct);
            await auditLog.AppendAsync(AuditLog.Record(AuditAction.UserLoggedOut), ct);
        }
    }

    private static string HashToken(string raw) =>
        Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));
}
