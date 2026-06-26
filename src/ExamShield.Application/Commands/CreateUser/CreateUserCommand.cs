using ExamShield.Domain.Enums;
using MediatR;

namespace ExamShield.Application.Commands.CreateUser;

public sealed record CreateUserCommand(string Email, string Password, UserRole Role)
    : IRequest<CreateUserResult>;

public sealed record CreateUserResult(Guid UserId);
