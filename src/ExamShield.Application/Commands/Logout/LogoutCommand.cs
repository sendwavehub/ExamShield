using MediatR;

namespace ExamShield.Application.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest;

