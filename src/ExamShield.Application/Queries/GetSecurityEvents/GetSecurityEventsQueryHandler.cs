using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Queries.GetSecurityEvents;

public sealed class GetSecurityEventsQueryHandler(ISecurityEventRepository repo)
    : IRequestHandler<GetSecurityEventsQuery, GetSecurityEventsResult>
{
    public async Task<GetSecurityEventsResult> Handle(GetSecurityEventsQuery request, CancellationToken ct)
    {
        var events = await repo.ListRecentAsync(request.Limit, ct);
        var dtos = events.Select(e => new SecurityEventDto(
            e.Id, e.EventType.ToString(), e.Severity.ToString(),
            e.Message, e.UserId, e.IpAddress, e.CaptureId, e.OccurredAt
        )).ToList();
        return new GetSecurityEventsResult(dtos);
    }
}
