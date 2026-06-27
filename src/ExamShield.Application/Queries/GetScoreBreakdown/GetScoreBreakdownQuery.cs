using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Queries.GetScoreBreakdown;

public sealed record QuestionBreakdown(
    int QuestionNumber,
    string StudentAnswer,
    string ExpectedAnswer,
    bool IsCorrect,
    string AnswerSource);

public sealed record GetScoreBreakdownResult(
    Guid CaptureId,
    Guid StudentId,
    Guid ExamId,
    int CorrectAnswers,
    int TotalQuestions,
    double Percentage,
    IReadOnlyList<QuestionBreakdown> Questions);

public sealed record GetScoreBreakdownQuery(Guid CaptureId) : IRequest<GetScoreBreakdownResult>;

public sealed class GetScoreBreakdownQueryHandler(
    IScoreRepository scores,
    ICaptureRepository captures,
    IOcrResultRepository ocrResults,
    IManualReviewRepository reviews,
    IAnswerKeyRepository answerKeys)
    : IRequestHandler<GetScoreBreakdownQuery, GetScoreBreakdownResult>
{
    public async Task<GetScoreBreakdownResult> Handle(GetScoreBreakdownQuery request, CancellationToken ct)
    {
        var captureId = new CaptureId(request.CaptureId);

        var score = await scores.GetByCaptureIdAsync(captureId, ct)
            ?? throw new KeyNotFoundException($"No score found for capture {request.CaptureId}.");

        var capture = await captures.GetByIdAsync(captureId, ct)
            ?? throw new KeyNotFoundException($"Capture {request.CaptureId} not found.");

        var answerKey = await answerKeys.GetEntityByExamIdAsync(capture.ExamId, ct)
            ?? throw new KeyNotFoundException($"No answer key found for exam {capture.ExamId.Value}.");

        var review = await reviews.GetByCaptureIdAsync(captureId, ct);

        IEnumerable<(int Question, string Answer, string Source)> studentAnswers;

        if (review is { Status: ManualReviewStatus.Completed or ManualReviewStatus.Approved })
        {
            studentAnswers = review.ReviewedAnswers
                .Select(a => (a.QuestionNumber, a.Text, "ManualReview"));
        }
        else
        {
            var ocrResult = await ocrResults.GetByCaptureIdAsync(captureId, ct)
                ?? throw new KeyNotFoundException($"No OCR result for capture {request.CaptureId}.");
            studentAnswers = ocrResult.Answers
                .Select(a => (a.QuestionNumber, a.Text, "OCR"));
        }

        var keyAnswers = answerKey.Answers;
        var breakdown = studentAnswers
            .OrderBy(a => a.Question)
            .Select(a => new QuestionBreakdown(
                a.Question,
                a.Answer,
                keyAnswers.TryGetValue(a.Question, out var expected) ? expected : string.Empty,
                keyAnswers.TryGetValue(a.Question, out var exp) &&
                    string.Equals(a.Answer, exp, StringComparison.OrdinalIgnoreCase),
                a.Source))
            .ToList();

        return new GetScoreBreakdownResult(
            score.CaptureId.Value, score.StudentId.Value, score.ExamId.Value,
            score.CorrectAnswers, score.TotalQuestions, score.Percentage,
            breakdown);
    }
}
