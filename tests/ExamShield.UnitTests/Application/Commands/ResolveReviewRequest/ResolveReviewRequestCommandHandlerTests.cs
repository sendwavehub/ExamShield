using ExamShield.Application.Commands.ResolveReviewRequest;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.ResolveReviewRequest;

public sealed class ResolveReviewRequestCommandHandlerTests
{
    private readonly IReviewRequestRepository _repo = Substitute.For<IReviewRequestRepository>();
    private readonly ResolveReviewRequestCommandHandler _sut;

    public ResolveReviewRequestCommandHandlerTests() =>
        _sut = new ResolveReviewRequestCommandHandler(_repo);

    private static ReviewRequest MakePending() =>
        ReviewRequest.Submit(
            new StudentId(Guid.NewGuid()),
            new CaptureId(Guid.NewGuid()),
            "Bubble B was shaded but read as A");

    [Fact]
    public async Task Handle_ValidRequest_SetsStatusResolved()
    {
        var rr = MakePending();
        _repo.GetByIdAsync(new ReviewRequestId(rr.Id.Value), default).Returns(rr);

        await _sut.Handle(new ResolveReviewRequestCommand(rr.Id.Value, "Confirmed — answer is B"), default);

        rr.Status.Should().Be(ReviewRequestStatus.Resolved);
        rr.ResolutionNote.Should().Be("Confirmed — answer is B");
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistsUpdate()
    {
        var rr = MakePending();
        _repo.GetByIdAsync(new ReviewRequestId(rr.Id.Value), default).Returns(rr);

        await _sut.Handle(new ResolveReviewRequestCommand(rr.Id.Value, "Verified correct"), default);

        await _repo.Received(1).UpdateAsync(rr, default);
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsReviewRequestNotFoundException()
    {
        var missing = Guid.NewGuid();
        _repo.GetByIdAsync(new ReviewRequestId(missing), default).Returns((ReviewRequest?)null);

        var act = () => _sut.Handle(new ResolveReviewRequestCommand(missing, "note"), default);

        await act.Should().ThrowAsync<ReviewRequestNotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyResolved_ThrowsInvalidOperationException()
    {
        var rr = MakePending();
        rr.Resolve("First resolution");
        _repo.GetByIdAsync(new ReviewRequestId(rr.Id.Value), default).Returns(rr);

        var act = () => _sut.Handle(new ResolveReviewRequestCommand(rr.Id.Value, "Second resolution"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been closed*");
    }

    [Fact]
    public async Task Handle_AlreadyRejected_ThrowsInvalidOperationException()
    {
        var rr = MakePending();
        rr.Reject("Rejected");
        _repo.GetByIdAsync(new ReviewRequestId(rr.Id.Value), default).Returns(rr);

        var act = () => _sut.Handle(new ResolveReviewRequestCommand(rr.Id.Value, "Resolving rejected"), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_EmptyNote_ThrowsArgumentException()
    {
        var rr = MakePending();
        _repo.GetByIdAsync(new ReviewRequestId(rr.Id.Value), default).Returns(rr);

        var act = () => _sut.Handle(new ResolveReviewRequestCommand(rr.Id.Value, ""), default);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhitespaceNote_ThrowsArgumentException()
    {
        var rr = MakePending();
        _repo.GetByIdAsync(new ReviewRequestId(rr.Id.Value), default).Returns(rr);

        var act = () => _sut.Handle(new ResolveReviewRequestCommand(rr.Id.Value, "   "), default);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
