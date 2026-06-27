using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Queries.ListActiveSessions;

public sealed record ListActiveSessionsQuery(Guid UserId) : IRequest<ListActiveSessionsResult>;

public sealed record SessionDto(Guid Id, DateTimeOffset ExpiresAt, DateTimeOffset CreatedAt);

public sealed record ListActiveSessionsResult(IReadOnlyList<SessionDto> Sessions);

public sealed class ListActiveSessionsQueryHandler(IRefreshTokenRepository tokens)
    : IRequestHandler<ListActiveSessionsQuery, ListActiveSessionsResult>
{
    public async Task<ListActiveSessionsResult> Handle(ListActiveSessionsQuery request, CancellationToken ct)
    {
        var active = await tokens.ListActiveByUserAsync(new UserId(request.UserId), ct);
        var dtos = active.Select(t => new SessionDto(t.Id, t.ExpiresAt, t.CreatedAt)).ToList();
        return new ListActiveSessionsResult(dtos);
    }
}
