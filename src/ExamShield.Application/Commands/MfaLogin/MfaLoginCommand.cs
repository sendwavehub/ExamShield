using ExamShield.Application.Commands.Login;
using MediatR;

namespace ExamShield.Application.Commands.MfaLogin;

public sealed record MfaLoginCommand(string Email, string Password, string Code)
    : IRequest<LoginResult>;
