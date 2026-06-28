using ExamShield.Application.Commands.RejectReviewRequest;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.RejectReviewRequest;

public sealed class RejectReviewRequestCommandHandlerTests
{
    private readonly IReviewRequestRepository _repo = Substitute.For<IReviewRequestRepository>();
    private readonly RejectReviewRequestCommandHandler _sut;

    public RejectReviewRequestCommandHandlerTests() =>
        _sut = new RejectReviewRequestCommandHandler(_repo);

    private static ReviewRequest MakePending() =>
        ReviewRequest.Submit(
            new StudentId(Guid.NewGuid()),
            new CaptureId(Guid.NewGuid()),
            "OCR misread question 3");

    [Fact]
    public async Task Handle_ValidRequest_SetsStatusRejected()
    {
        var rr = MakePending();
        _repo.GetByIdAsync(new ReviewRequestId(rr.Id.Value), default).Returns(rr);

        await _sut.Handle(new RejectReviewRequestCommand(rr.Id.Value, "Claim unsupported by evidence"), default);

        rr.Status.Should().Be(ReviewRequestStatus.Rejected);
        rr.ResolutionNote.Should().Be("Claim unsupported by evidence");
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistsUpdate()
    {
        var rr = MakePending();
        _repo.GetByIdAsync(new ReviewRequestId(rr.Id.Value), default).Returns(rr);

        await _sut.Handle(new RejectReviewRequestCommand(rr.Id.Value, "No evidence of error"), default);

        await _repo.Received(1).UpdateAsync(rr, default);
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsReviewRequestNotFoundException()
    {
        var missing = Guid.NewGuid();
        _repo.GetByIdAsync(new ReviewRequestId(missing), default).Returns((ReviewRequest?)null);

        var act = () => _sut.Handle(new RejectReviewRequestCommand(missing, "reason"), default);

        await act.Should().ThrowAsync<ReviewRequestNotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyResolved_ThrowsInvalidOperationException()
    {
        var rr = MakePending();
        rr.Resolve("Previously resolved");
        _repo.GetByIdAsync(new ReviewRequestId(rr.Id.Value), default).Returns(rr);

        var act = () => _sut.Handle(new RejectReviewRequestCommand(rr.Id.Value, "Late rejection"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been closed*");
    }

    [Fact]
    public async Task Handle_AlreadyRejected_ThrowsInvalidOperationException()
    {
        var rr = MakePending();
        rr.Reject("First rejection");
        _repo.GetByIdAsync(new ReviewRequestId(rr.Id.Value), default).Returns(rr);

        var act = () => _sut.Handle(new RejectReviewRequestCommand(rr.Id.Value, "Second rejection"), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_EmptyReason_ThrowsArgumentException()
    {
        var rr = MakePending();
        _repo.GetByIdAsync(new ReviewRequestId(rr.Id.Value), default).Returns(rr);

        var act = () => _sut.Handle(new RejectReviewRequestCommand(rr.Id.Value, "   "), default);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
