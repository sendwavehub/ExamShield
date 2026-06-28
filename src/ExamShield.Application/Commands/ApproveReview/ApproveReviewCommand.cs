using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.ApproveReview;

public sealed record ApproveReviewCommand(Guid ReviewId, Guid SupervisorId) : IRequest;

public sealed class ApproveReviewCommandHandler(
    IManualReviewRepository reviews,
    IAuditLogRepository auditLog)
    : IRequestHandler<ApproveReviewCommand>
{
    public async Task Handle(ApproveReviewCommand command, CancellationToken ct)
    {
        var review = await reviews.GetByIdAsync(new ManualReviewId(command.ReviewId), ct)
            ?? throw new ManualReviewNotFoundException(command.ReviewId);

        review.Approve(new UserId(command.SupervisorId));
        await reviews.UpdateAsync(review, ct);
        await auditLog.AppendAsync(AuditLog.Record(AuditAction.ReviewApproved), ct);
    }
}
