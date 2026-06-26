using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Queries.GetAuditLog;

public sealed class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, GetAuditLogResult>
{
    private readonly IAuditLogRepository _repository;

    public GetAuditLogQueryHandler(IAuditLogRepository repository) =>
        _repository = repository;

    public async Task<GetAuditLogResult> Handle(GetAuditLogQuery query, CancellationToken ct)
    {
        var captureId = query.CaptureId.HasValue ? new CaptureId(query.CaptureId.Value) : null;

        var (entries, total) = await _repository.QueryAsync(captureId, query.Page, query.PageSize, ct);

        var dtos = entries.Select(e => new AuditLogDto(
            e.Id.Value,
            e.Action.ToString(),
            e.CaptureId?.Value,
            e.UserId,
            e.IpAddress,
            e.OccurredAt,
            e.Reason
        )).ToList();

        return new GetAuditLogResult(dtos, total);
    }
}
