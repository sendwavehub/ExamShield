using MediatR;

namespace ExamShield.Application.Queries.GetOcrQueue;

public sealed record OcrQueueItem(
    Guid CaptureId,
    Guid ExamId,
    Guid StudentId,
    DateTimeOffset UploadedAt);

public sealed record GetOcrQueueResult(IReadOnlyList<OcrQueueItem> Items);

public sealed record GetOcrQueueQuery : IRequest<GetOcrQueueResult>;
