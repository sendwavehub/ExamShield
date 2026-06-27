using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Queries.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IRequest<GetProfileResult>;

public sealed record GetProfileResult(string Email, string Role, bool MfaEnabled);

public sealed class GetProfileQueryHandler(IUserRepository users)
    : IRequestHandler<GetProfileQuery, GetProfileResult>
{
    public async Task<GetProfileResult> Handle(GetProfileQuery request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(new UserId(request.UserId), ct)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found.");
        return new GetProfileResult(user.Email.Value, user.Role.ToString(), user.MfaEnabled);
    }
}
