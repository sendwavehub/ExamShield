using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.SubmitReview;

public sealed class SubmitReviewCommandHandler : IRequestHandler<SubmitReviewCommand>
{
    private readonly IManualReviewRepository _reviews;
    private readonly IAuditLogRepository _auditLog;

    public SubmitReviewCommandHandler(IManualReviewRepository reviews, IAuditLogRepository auditLog)
    {
        _reviews = reviews;
        _auditLog = auditLog;
    }

    public async Task Handle(SubmitReviewCommand command, CancellationToken ct)
    {
        var review = await _reviews.GetByIdAsync(new ManualReviewId(command.ReviewId), ct)
            ?? throw new ManualReviewNotFoundException(command.ReviewId);

        var answers = command.Answers
            .Select(a => new ReviewedAnswer(a.QuestionNumber, a.Text))
            .ToList();

        review.Complete(answers, new UserId(command.ReviewedByUserId));

        await _reviews.UpdateAsync(review, ct);
        await _auditLog.AppendAsync(
            AuditLog.Record(AuditAction.ManualReviewCompleted, captureId: review.CaptureId), ct);
    }
}
