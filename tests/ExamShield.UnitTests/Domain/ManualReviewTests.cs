using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain;

public sealed class ManualReviewTests
{
    private static ManualReview PendingReview()
    {
        var ocr = OcrResult.Create(CaptureId.New(),
        [
            new ExtractedAnswer(1, "A", new OcrConfidence(0.50))
        ]);
        return ManualReview.CreateFor(ocr);
    }

    private static IReadOnlyList<ReviewedAnswer> OneAnswer() =>
        [new ReviewedAnswer(1, "B")];

    [Fact]
    public void Complete_WhenPending_SetsCompletedStatus()
    {
        var review = PendingReview();

        review.Complete(OneAnswer(), UserId.New());

        review.Status.Should().Be(ManualReviewStatus.Completed);
    }

    [Fact]
    public void Complete_WhenPending_StoresReviewedAnswers()
    {
        var review = PendingReview();

        review.Complete(OneAnswer(), UserId.New());

        review.ReviewedAnswers.Should().HaveCount(1);
        review.ReviewedAnswers[0].QuestionNumber.Should().Be(1);
        review.ReviewedAnswers[0].Text.Should().Be("B");
    }

    [Fact]
    public void Complete_WhenPending_RecordsReviewerId()
    {
        var review = PendingReview();
        var reviewerId = UserId.New();

        review.Complete(OneAnswer(), reviewerId);

        review.ReviewedBy.Should().Be(reviewerId);
    }

    [Fact]
    public void Complete_WhenPending_SetsCompletedAt()
    {
        var review = PendingReview();

        review.Complete(OneAnswer(), UserId.New());

        review.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ThrowsInvalidOperationException()
    {
        var review = PendingReview();
        review.Complete(OneAnswer(), UserId.New());

        var act = () => review.Complete(OneAnswer(), UserId.New());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_WithEmptyAnswers_ThrowsArgumentException()
    {
        var review = PendingReview();

        var act = () => review.Complete([], UserId.New());

        act.Should().Throw<ArgumentException>();
    }
}
