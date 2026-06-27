using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Queries.GetUserById;

public sealed record GetUserByIdResult(
    Guid UserId, string Email, string? DisplayName,
    string Role, bool IsActive, bool MfaEnabled, DateTimeOffset CreatedAt);

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<GetUserByIdResult?>;

public sealed class GetUserByIdQueryHandler(IUserRepository users)
    : IRequestHandler<GetUserByIdQuery, GetUserByIdResult?>
{
    public async Task<GetUserByIdResult?> Handle(GetUserByIdQuery query, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(new UserId(query.UserId), ct);
        if (user is null) return null;
        return new GetUserByIdResult(
            user.Id.Value, user.Email.Value, user.DisplayName,
            user.Role.ToString(), user.IsActive, user.MfaEnabled, user.CreatedAt);
    }
}
