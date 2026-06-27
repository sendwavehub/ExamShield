using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Queries.GetOcrQueue;

public sealed class GetOcrQueueQueryHandler(ICaptureRepository captures)
    : IRequestHandler<GetOcrQueueQuery, GetOcrQueueResult>
{
    public async Task<GetOcrQueueResult> Handle(GetOcrQueueQuery request, CancellationToken ct)
    {
        var pending = await captures.ListByStatusAsync(CaptureStatus.Uploaded, ct);

        var items = pending
            .Select(c => new OcrQueueItem(c.Id.Value, c.ExamId.Value, c.StudentId.Value, c.CapturedAt))
            .ToList();

        return new GetOcrQueueResult(items);
    }
}
