using ExamShield.Application.Interfaces;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email, string ResetBaseUrl = "") : IRequest;

public sealed class ForgotPasswordCommandHandler(
    IUserRepository users,
    IPasswordResetTokenRepository tokens,
    IEmailSender email)
    : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(new Email(command.Email), ct);
        if (user is null)
            return; // silent — no user enumeration

        var token = PasswordResetToken.Create(command.Email);
        await tokens.AddAsync(token, ct);

        var resetUrl = string.IsNullOrEmpty(command.ResetBaseUrl)
            ? $"https://examshield.local/reset-password?token={token.Token}"
            : $"{command.ResetBaseUrl}?token={token.Token}";

        var body = $"""
            You requested a password reset for your ExamShield account.

            Click the link below to reset your password (valid for 1 hour):
            {resetUrl}

            If you did not request this, please ignore this email.
            """;

        await email.SendAsync(command.Email, "Reset your ExamShield password", body, ct);
    }
}
