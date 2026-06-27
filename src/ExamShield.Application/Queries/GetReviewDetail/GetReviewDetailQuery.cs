using MediatR;

namespace ExamShield.Application.Queries.GetReviewDetail;

public sealed record GetReviewDetailQuery(Guid ReviewId) : IRequest<ReviewDetailResult>;

public sealed record OcrAnswerDto(int QuestionNumber, string Text, double Confidence);

public sealed record ReviewDetailResult(
    Guid ReviewId,
    Guid CaptureId,
    Guid OcrResultId,
    string Status,
    IReadOnlyList<OcrAnswerDto> OcrAnswers,
    DateTimeOffset CreatedAt
);
