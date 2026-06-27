using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Queries.GetCaptures;

public sealed class GetCapturesQueryHandler(ICaptureRepository captures)
    : IRequestHandler<GetCapturesQuery, GetCapturesResult>
{
    public async Task<GetCapturesResult> Handle(GetCapturesQuery request, CancellationToken ct)
    {
        var all = await captures.ListAllAsync(ct);
        var dtos = all
            .Select(c => new CaptureDto(
                c.Id.Value, c.ExamId.Value, c.StudentId.Value, c.DeviceId.Value,
                c.Status.ToString(), c.CapturedAt, c.StorageKey))
            .ToList();
        return new GetCapturesResult(dtos);
    }
}
