using MediatR;

namespace ExamShield.Application.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public sealed record LoginResult(string Token, string Role);
