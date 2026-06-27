using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Queries.GetUsers;

public sealed class GetUsersQueryHandler(IUserRepository users)
    : IRequestHandler<GetUsersQuery, GetUsersResult>
{
    public async Task<GetUsersResult> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var all = await users.ListAllAsync(ct);
        var dtos = all
            .OrderBy(u => u.Email.Value)
            .Select(u => new UserDto(u.Id.Value, u.Email.Value, u.Role.ToString(), u.IsActive, u.CreatedAt))
            .ToList();
        return new GetUsersResult(dtos);
    }
}
