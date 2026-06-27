using MediatR;

namespace ExamShield.Application.Queries.GetUsers;

public sealed record UserDto(
    Guid UserId, string Email, string Role, bool IsActive, DateTimeOffset CreatedAt);

public sealed record GetUsersResult(IReadOnlyList<UserDto> Users);

public sealed record GetUsersQuery : IRequest<GetUsersResult>;
