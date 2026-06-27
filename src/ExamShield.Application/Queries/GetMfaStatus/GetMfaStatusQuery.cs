using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Queries.GetMfaStatus;

public sealed record GetMfaStatusResult(bool MfaEnabled);
public sealed record GetMfaStatusQuery(Guid UserId) : IRequest<GetMfaStatusResult>;

public sealed class GetMfaStatusQueryHandler(IUserRepository users)
    : IRequestHandler<GetMfaStatusQuery, GetMfaStatusResult>
{
    public async Task<GetMfaStatusResult> Handle(GetMfaStatusQuery q, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(new UserId(q.UserId), ct)
            ?? throw new InvalidOperationException("User not found.");
        return new GetMfaStatusResult(user.MfaEnabled);
    }
}
