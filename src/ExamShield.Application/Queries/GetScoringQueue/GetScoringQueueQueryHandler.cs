using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Queries.GetScoringQueue;

public sealed class GetScoringQueueQueryHandler(
    IOcrResultRepository ocrResults,
    IScoreRepository scores,
    ICaptureRepository captures)
    : IRequestHandler<GetScoringQueueQuery, GetScoringQueueResult>
{
    public async Task<GetScoringQueueResult> Handle(GetScoringQueueQuery request, CancellationToken ct)
    {
        var completed = await ocrResults.ListCompletedAsync(ct);

        var items = new List<ScoringQueueItem>();
        foreach (var ocr in completed)
        {
            if (await scores.ExistsByCaptureIdAsync(ocr.CaptureId, ct))
                continue;

            var capture = await captures.GetByIdAsync(ocr.CaptureId, ct);
            var examId = capture?.ExamId.Value ?? Guid.Empty;

            items.Add(new ScoringQueueItem(
                ocr.CaptureId.Value, examId, ocr.Id.Value,
                ocr.Status.ToString(), ocr.OverallConfidence.Value, ocr.ProcessedAt));
        }

        return new GetScoringQueueResult(items);
    }
}
