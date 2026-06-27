using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.RejectReviewRequest;

public sealed record RejectReviewRequestCommand(Guid ReviewRequestId, string Reason) : IRequest;

public sealed class RejectReviewRequestCommandHandler(IReviewRequestRepository repo)
    : IRequestHandler<RejectReviewRequestCommand>
{
    public async Task Handle(RejectReviewRequestCommand command, CancellationToken ct)
    {
        var rr = await repo.GetByIdAsync(new ReviewRequestId(command.ReviewRequestId), ct)
            ?? throw new ReviewRequestNotFoundException(command.ReviewRequestId);

        rr.Reject(command.Reason);
        await repo.UpdateAsync(rr, ct);
    }
}
