using MediatR;

namespace ExamShield.Application.Commands.UpdateUserRole;

public sealed record UpdateUserRoleCommand(Guid UserId, string NewRole) : IRequest;
