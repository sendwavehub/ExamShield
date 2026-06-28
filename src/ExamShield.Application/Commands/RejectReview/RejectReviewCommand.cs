using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.RejectReview;

public sealed record RejectReviewCommand(Guid ReviewId, Guid SupervisorId, string Reason) : IRequest;

public sealed class RejectReviewCommandHandler(
    IManualReviewRepository reviews,
    IAuditLogRepository auditLog)
    : IRequestHandler<RejectReviewCommand>
{
    public async Task Handle(RejectReviewCommand command, CancellationToken ct)
    {
        var review = await reviews.GetByIdAsync(new ManualReviewId(command.ReviewId), ct)
            ?? throw new ManualReviewNotFoundException(command.ReviewId);

        review.Reject(command.Reason, new UserId(command.SupervisorId));
        await reviews.UpdateAsync(review, ct);
        await auditLog.AppendAsync(AuditLog.Record(AuditAction.ReviewRejected), ct);
    }
}
