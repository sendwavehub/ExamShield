using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Queries.GetOcrResult;

public sealed record GetOcrResultQuery(Guid CaptureId) : IRequest<GetOcrResultResult>;

public sealed record OcrAnswerDto(int QuestionNumber, string Text, double Confidence);

public sealed record GetOcrResultResult(
    Guid OcrResultId,
    Guid CaptureId,
    OcrStatus Status,
    double OverallConfidence,
    bool RequiresManualReview,
    IReadOnlyList<OcrAnswerDto> Answers);
