using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Queries.GetReviewDetail;

public sealed class GetReviewDetailQueryHandler(
    IManualReviewRepository reviews,
    IOcrResultRepository ocrResults)
    : IRequestHandler<GetReviewDetailQuery, ReviewDetailResult>
{
    public async Task<ReviewDetailResult> Handle(GetReviewDetailQuery request, CancellationToken ct)
    {
        var review = await reviews.GetByIdAsync(new ManualReviewId(request.ReviewId), ct)
            ?? throw new ManualReviewNotFoundException(request.ReviewId);

        var ocr = await ocrResults.GetByIdAsync(review.OcrResultId, ct);

        var answers = ocr?.Answers
            .Select(a => new OcrAnswerDto(a.QuestionNumber, a.Text, a.Confidence.Value))
            .ToList()
            ?? [];

        return new ReviewDetailResult(
            review.Id.Value,
            review.CaptureId.Value,
            review.OcrResultId.Value,
            review.Status.ToString(),
            answers,
            review.CreatedAt
        );
    }
}
