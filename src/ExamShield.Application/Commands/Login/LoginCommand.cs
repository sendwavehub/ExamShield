using MediatR;

namespace ExamShield.Application.Commands.Login;

public sealed record LoginCommand(string Email, string Password, string? IpAddress = null) : IRequest<LoginResult>;

public sealed record LoginResult(string Token, string RefreshToken, string Role, bool RequiresMfa = false);
