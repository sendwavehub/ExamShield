using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.UpdateUserRole;

public sealed class UpdateUserRoleCommandHandler(IUserRepository users)
    : IRequestHandler<UpdateUserRoleCommand>
{
    public async Task Handle(UpdateUserRoleCommand request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(new UserId(request.UserId), ct)
            ?? throw new UserNotFoundException(request.UserId);

        if (!Enum.TryParse<UserRole>(request.NewRole, ignoreCase: true, out var role))
            throw new ArgumentException($"Invalid role '{request.NewRole}'.");

        user.ChangeRole(role);
        await users.SaveAsync(user, ct);
    }
}
