using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.RevokeSession;

public sealed record RevokeSessionCommand(Guid UserId, Guid SessionId) : IRequest;

public sealed class RevokeSessionCommandHandler(IRefreshTokenRepository tokens)
    : IRequestHandler<RevokeSessionCommand>
{
    public async Task Handle(RevokeSessionCommand command, CancellationToken ct)
    {
        var token = await tokens.FindByIdAsync(command.SessionId, ct)
            ?? throw new KeyNotFoundException($"Session {command.SessionId} not found.");

        if (token.UserId != new UserId(command.UserId))
            throw new UnauthorizedAccessException("Cannot revoke another user's session.");

        token.Revoke();
        await tokens.SaveAsync(token, ct);
    }
}
