using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.ReactivateUser;

public sealed record ReactivateUserCommand(Guid UserId) : IRequest;

public sealed class ReactivateUserCommandHandler(IUserRepository users)
    : IRequestHandler<ReactivateUserCommand>
{
    public async Task Handle(ReactivateUserCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(new UserId(command.UserId), ct)
            ?? throw new UserNotFoundException(command.UserId);

        user.Reactivate();
        await users.SaveAsync(user, ct);
    }
}
