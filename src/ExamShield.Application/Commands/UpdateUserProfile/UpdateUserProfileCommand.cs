using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.UpdateUserProfile;

public sealed record UpdateUserProfileCommand(Guid UserId, string? DisplayName) : IRequest;

public sealed class UpdateUserProfileCommandHandler(IUserRepository users)
    : IRequestHandler<UpdateUserProfileCommand>
{
    public async Task Handle(UpdateUserProfileCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(new UserId(command.UserId), ct)
            ?? throw new KeyNotFoundException($"User {command.UserId} not found.");

        user.UpdateProfile(command.DisplayName);
        await users.SaveAsync(user, ct);
    }
}
