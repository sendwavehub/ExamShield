using MediatR;

namespace ExamShield.Application.Queries.GetPendingReviews;

public sealed record GetPendingReviewsQuery : IRequest<GetPendingReviewsResult>;

public sealed record PendingReviewDto(
    Guid ReviewId,
    Guid CaptureId,
    Guid OcrResultId,
    DateTimeOffset CreatedAt);

public sealed record GetPendingReviewsResult(IReadOnlyList<PendingReviewDto> Reviews);
