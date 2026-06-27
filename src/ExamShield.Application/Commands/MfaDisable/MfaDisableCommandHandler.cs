using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.MfaDisable;

public sealed class MfaDisableCommandHandler(IUserRepository users)
    : IRequestHandler<MfaDisableCommand, MfaDisableResult>
{
    public async Task<MfaDisableResult> Handle(MfaDisableCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(new UserId(cmd.UserId), ct)
            ?? throw new InvalidOperationException("User not found.");

        user.DisableMfa();
        await users.SaveAsync(user, ct);
        return new MfaDisableResult(false);
    }
}
