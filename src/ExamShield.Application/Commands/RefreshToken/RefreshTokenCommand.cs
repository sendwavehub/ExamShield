using MediatR;

namespace ExamShield.Application.Commands.Refresh;

public sealed record RefreshTokenResult(string Token, string RefreshToken, string Role);
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<RefreshTokenResult>;
