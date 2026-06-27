using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class ManualReviewEscalationTests
{
    private static ManualReview CompletedReview()
    {
        var ocr    = OcrResult.Create(CaptureId.New(), [new ExtractedAnswer(1, "A", new OcrConfidence(0.3))]);
        var review = ManualReview.CreateFor(ocr);
        review.Complete([new ReviewedAnswer(1, "B")], UserId.New());
        return review;
    }

    [Fact]
    public void Escalate_CompletedReview_ChangesStatusToEscalated()
    {
        var review = CompletedReview();

        review.Escalate("Cannot determine answer — handwriting ambiguous", UserId.New());

        Assert.Equal(ManualReviewStatus.Escalated, review.Status);
    }

    [Fact]
    public void Escalate_StoresReasonAndSupervisor()
    {
        var review       = CompletedReview();
        var supervisorId = UserId.New();
        const string reason = "Disputed — needs senior review";

        review.Escalate(reason, supervisorId);

        Assert.Equal(reason,       review.EscalationReason);
        Assert.Equal(supervisorId, review.SupervisorId);
        Assert.NotNull(review.SupervisedAt);
    }

    [Fact]
    public void Escalate_PendingReview_ThrowsInvalidOperation()
    {
        var ocr    = OcrResult.Create(CaptureId.New(), [new ExtractedAnswer(1, "A", new OcrConfidence(0.3))]);
        var review = ManualReview.CreateFor(ocr);

        Assert.Throws<InvalidOperationException>(() =>
            review.Escalate("reason", UserId.New()));
    }

    [Fact]
    public void Escalate_AlreadyEscalatedReview_ThrowsInvalidOperation()
    {
        var review = CompletedReview();
        review.Escalate("first time", UserId.New());

        Assert.Throws<InvalidOperationException>(() =>
            review.Escalate("again", UserId.New()));
    }

    [Fact]
    public void Escalate_EmptyReason_ThrowsArgumentException()
    {
        var review = CompletedReview();

        Assert.Throws<ArgumentException>(() => review.Escalate("", UserId.New()));
    }
}
