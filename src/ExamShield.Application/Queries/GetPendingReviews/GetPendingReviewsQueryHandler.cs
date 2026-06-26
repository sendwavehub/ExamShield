using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Queries.GetPendingReviews;

public sealed class GetPendingReviewsQueryHandler : IRequestHandler<GetPendingReviewsQuery, GetPendingReviewsResult>
{
    private readonly IManualReviewRepository _reviews;

    public GetPendingReviewsQueryHandler(IManualReviewRepository reviews) => _reviews = reviews;

    public async Task<GetPendingReviewsResult> Handle(GetPendingReviewsQuery query, CancellationToken ct)
    {
        var reviews = await _reviews.GetPendingAsync(ct);
        var dtos = reviews
            .Select(r => new PendingReviewDto(r.Id.Value, r.CaptureId.Value, r.OcrResultId.Value, r.CreatedAt))
            .ToList();
        return new GetPendingReviewsResult(dtos);
    }
}
