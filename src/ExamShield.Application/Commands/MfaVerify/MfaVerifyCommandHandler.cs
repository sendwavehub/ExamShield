using ExamShield.Application.Interfaces;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.MfaVerify;

public sealed class MfaVerifyCommandHandler(IUserRepository users, ITotpService totp)
    : IRequestHandler<MfaVerifyCommand, MfaVerifyResult>
{
    public async Task<MfaVerifyResult> Handle(MfaVerifyCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(new UserId(cmd.UserId), ct)
            ?? throw new InvalidOperationException("User not found.");

        if (user.MfaSecret is null)
            throw new InvalidOperationException("MFA setup not initiated.");

        if (!totp.Verify(user.MfaSecret, cmd.Code))
            throw new UnauthorizedAccessException("Invalid TOTP code.");

        user.EnableMfa();
        await users.SaveAsync(user, ct);
        return new MfaVerifyResult(true);
    }
}
