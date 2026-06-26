using ExamShield.Domain.Enums;

namespace ExamShield.Api.Contracts;

public sealed record TriggerOcrResponse(Guid OcrResultId, string Status, bool RequiresManualReview);

public sealed record OcrAnswerResponse(int QuestionNumber, string Text, double Confidence);

public sealed record GetOcrResultResponse(
    Guid OcrResultId,
    Guid CaptureId,
    string Status,
    double OverallConfidence,
    bool RequiresManualReview,
    IReadOnlyList<OcrAnswerResponse> Answers);
