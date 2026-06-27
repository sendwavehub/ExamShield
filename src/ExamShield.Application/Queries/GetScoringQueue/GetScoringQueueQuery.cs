using MediatR;

namespace ExamShield.Application.Queries.GetScoringQueue;

public sealed record ScoringQueueItem(
    Guid CaptureId, Guid ExamId, Guid OcrResultId,
    string OcrStatus, double OverallConfidence, DateTimeOffset CompletedAt);

public sealed record GetScoringQueueResult(IReadOnlyList<ScoringQueueItem> Items);

public sealed record GetScoringQueueQuery : IRequest<GetScoringQueueResult>;
