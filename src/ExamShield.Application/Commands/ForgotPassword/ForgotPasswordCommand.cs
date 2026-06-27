using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest;

public sealed class ForgotPasswordCommandHandler(
    IUserRepository users,
    IPasswordResetTokenRepository tokens)
    : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(new Email(command.Email), ct);
        if (user is null)
            return; // silent — no user enumeration

        var token = PasswordResetToken.Create(command.Email);
        await tokens.AddAsync(token, ct);
        // In production: send email with reset link containing token.Token
    }
}
