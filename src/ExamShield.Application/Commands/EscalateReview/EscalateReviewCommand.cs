using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.EscalateReview;

public sealed record EscalateReviewCommand(Guid ReviewId, Guid SupervisorId, string Reason) : IRequest;

public sealed class EscalateReviewCommandHandler(IManualReviewRepository reviews)
    : IRequestHandler<EscalateReviewCommand>
{
    public async Task Handle(EscalateReviewCommand command, CancellationToken ct)
    {
        var review = await reviews.GetByIdAsync(new ManualReviewId(command.ReviewId), ct)
            ?? throw new ManualReviewNotFoundException(command.ReviewId);

        review.Escalate(command.Reason, new UserId(command.SupervisorId));
        await reviews.UpdateAsync(review, ct);
    }
}
