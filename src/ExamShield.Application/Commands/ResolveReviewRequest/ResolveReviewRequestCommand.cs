using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.ResolveReviewRequest;

public sealed record ResolveReviewRequestCommand(Guid ReviewRequestId, string Note) : IRequest;

public sealed class ResolveReviewRequestCommandHandler(IReviewRequestRepository repo)
    : IRequestHandler<ResolveReviewRequestCommand>
{
    public async Task Handle(ResolveReviewRequestCommand command, CancellationToken ct)
    {
        var rr = await repo.GetByIdAsync(new ReviewRequestId(command.ReviewRequestId), ct)
            ?? throw new ReviewRequestNotFoundException(command.ReviewRequestId);

        rr.Resolve(command.Note);
        await repo.UpdateAsync(rr, ct);
    }
}
