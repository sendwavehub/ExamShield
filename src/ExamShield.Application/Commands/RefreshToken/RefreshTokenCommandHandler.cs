using System.Security.Cryptography;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Commands.Refresh;

public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    IJwtTokenService jwt)
    : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var hash = HashToken(cmd.RefreshToken);
        var stored = await refreshTokens.FindByHashAsync(hash, ct);

        if (stored is null || !stored.IsActive)
            throw new InvalidCredentialsException();

        var user = await users.GetByIdAsync(stored.UserId, ct)
            ?? throw new InvalidCredentialsException();

        if (!user.IsActive || user.IsLockedOut)
            throw new InvalidCredentialsException();

        // Revoke used token (rotation — single use)
        stored.Revoke();
        await refreshTokens.SaveAsync(stored, ct);

        // Issue new pair
        var rawToken = ExamShield.Application.Commands.Login.LoginCommandHandler.GenerateRawToken();
        var newToken = RefreshToken.Create(user.Id, HashToken(rawToken), expiryDays: 7);
        await refreshTokens.AddAsync(newToken, ct);

        return new RefreshTokenResult(jwt.Generate(user), rawToken, user.Role.ToString());
    }

    private static string HashToken(string raw) =>
        Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));
}
