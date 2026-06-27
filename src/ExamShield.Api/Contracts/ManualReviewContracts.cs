namespace ExamShield.Api.Contracts;

public sealed record ReviewedAnswerRequest(int QuestionNumber, string Text);

public sealed record SubmitReviewRequest(IReadOnlyList<ReviewedAnswerRequest> Answers);

public sealed record PendingReviewItem(
    Guid ReviewId, Guid CaptureId, Guid OcrResultId, DateTimeOffset CreatedAt);

public sealed record GetPendingReviewsResponse(IReadOnlyList<PendingReviewItem> Reviews);

public sealed record ReviewDetailResponse(
    Guid ReviewId,
    Guid CaptureId,
    Guid OcrResultId,
    string Status,
    IReadOnlyList<OcrAnswerResponse> OcrAnswers,
    DateTimeOffset CreatedAt
);
