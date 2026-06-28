using ExamShield.Application.Commands.SubmitReview;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.SubmitReview;

public sealed class SubmitReviewCommandHandlerTests
{
    private readonly IManualReviewRepository _reviews = Substitute.For<IManualReviewRepository>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly SubmitReviewCommandHandler _sut;

    public SubmitReviewCommandHandlerTests() => _sut = new(_reviews, _auditLog);

    private static ManualReview MakeReview()
    {
        var captureId = new CaptureId(Guid.NewGuid());
        var ocrResult = OcrResult.Create(captureId, [new ExtractedAnswer(1, "A", new OcrConfidence(0.3))]);
        return ManualReview.CreateFor(ocrResult);
    }

    [Fact]
    public async Task Handle_CompletesReviewAndPersists()
    {
        var review = MakeReview();
        _reviews.GetByIdAsync(Arg.Any<ManualReviewId>(), default).Returns(review);

        await _sut.Handle(new(review.Id.Value, [new(1, "A")], Guid.NewGuid()), default);

        review.Status.Should().Be(ManualReviewStatus.Completed);
        await _reviews.Received(1).UpdateAsync(review, default);
    }

    [Fact]
    public async Task Handle_AppendsAuditLog()
    {
        var review = MakeReview();
        _reviews.GetByIdAsync(Arg.Any<ManualReviewId>(), default).Returns(review);

        await _sut.Handle(new(review.Id.Value, [new(1, "A")], Guid.NewGuid()), default);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.ManualReviewCompleted), default);
    }

    [Fact]
    public async Task Handle_ReviewNotFound_ThrowsManualReviewNotFoundException()
    {
        _reviews.GetByIdAsync(Arg.Any<ManualReviewId>(), default)
            .Returns((ManualReview?)null);

        await FluentActions.Invoking(() =>
            _sut.Handle(new(Guid.NewGuid(), [], Guid.NewGuid()), default))
            .Should().ThrowAsync<ManualReviewNotFoundException>();
    }

    [Fact]
    public async Task Handle_SetsAnswersOnReview()
    {
        var review = MakeReview();
        _reviews.GetByIdAsync(Arg.Any<ManualReviewId>(), default).Returns(review);

        await _sut.Handle(new(review.Id.Value, [new(3, "C")], Guid.NewGuid()), default);

        review.ReviewedAnswers.Should().ContainSingle(a => a.QuestionNumber == 3 && a.Text == "C");
    }
}
