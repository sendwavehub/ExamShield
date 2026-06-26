using ExamShield.Application.Commands.SubmitReview;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands;

public sealed class SubmitReviewCommandHandlerTests
{
    private readonly IManualReviewRepository _reviews = Substitute.For<IManualReviewRepository>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly SubmitReviewCommandHandler _sut;

    public SubmitReviewCommandHandlerTests() =>
        _sut = new SubmitReviewCommandHandler(_reviews, _auditLog);

    private static ManualReview PendingReview()
    {
        var ocr = OcrResult.Create(CaptureId.New(),
        [
            new ExtractedAnswer(1, "A", new OcrConfidence(0.50))
        ]);
        return ManualReview.CreateFor(ocr);
    }

    private static List<ReviewedAnswerDto> OneAnswer() => [new ReviewedAnswerDto(1, "B")];

    [Fact]
    public async Task Handle_WithValidReview_CompletesReview()
    {
        var review = PendingReview();
        _reviews.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);

        await _sut.Handle(
            new SubmitReviewCommand(review.Id.Value, OneAnswer(), Guid.NewGuid()), default);

        review.Status.Should().Be(ManualReviewStatus.Completed);
    }

    [Fact]
    public async Task Handle_WithValidReview_UpdatesRepository()
    {
        var review = PendingReview();
        _reviews.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);

        await _sut.Handle(
            new SubmitReviewCommand(review.Id.Value, OneAnswer(), Guid.NewGuid()), default);

        await _reviews.Received(1).UpdateAsync(review, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidReview_AppendsAuditEntry()
    {
        var review = PendingReview();
        _reviews.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);

        await _sut.Handle(
            new SubmitReviewCommand(review.Id.Value, OneAnswer(), Guid.NewGuid()), default);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(e => e.Action == AuditAction.ManualReviewCompleted),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenReviewNotFound_ThrowsManualReviewNotFoundException()
    {
        _reviews.GetByIdAsync(Arg.Any<ManualReviewId>(), Arg.Any<CancellationToken>())
            .Returns((ManualReview?)null);

        await Assert.ThrowsAsync<ManualReviewNotFoundException>(() =>
            _sut.Handle(
                new SubmitReviewCommand(Guid.NewGuid(), OneAnswer(), Guid.NewGuid()), default));
    }
}
