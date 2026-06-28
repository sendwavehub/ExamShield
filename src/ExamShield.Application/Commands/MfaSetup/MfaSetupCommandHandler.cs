using ExamShield.Application.Interfaces;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.MfaSetup;

public sealed class MfaSetupCommandHandler(IUserRepository users, ITotpService totp)
    : IRequestHandler<MfaSetupCommand, MfaSetupResult>
{
    public async Task<MfaSetupResult> Handle(MfaSetupCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(new UserId(cmd.UserId), ct)
            ?? throw new InvalidOperationException("User not found.");

        if (user.MfaEnabled)
            throw new InvalidOperationException(
                "MFA is already enabled. Disable it first before re-enrolling.");

        var secret = totp.GenerateSecret();
        user.SetMfaSecret(secret);
        await users.SaveAsync(user, ct);

        var qrUri = totp.GetQrUri(secret, cmd.Email);
        return new MfaSetupResult(secret, qrUri);
    }
}
