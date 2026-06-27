using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class ManualReviewSupervisorTests
{
    private static ManualReview CompletedReview()
    {
        var ocrResult = OcrResult.Create(
            CaptureId.New(),
            [new ExtractedAnswer(1, "A", new OcrConfidence(0.3))]); // 0.3 < threshold → LowConfidence

        var review = ManualReview.CreateFor(ocrResult);
        review.Complete([new ReviewedAnswer(1, "B")], UserId.New());
        return review;
    }

    [Fact]
    public void Approve_CompletedReview_ChangesStatusToApproved()
    {
        var review = CompletedReview();

        review.Approve(UserId.New());

        Assert.Equal(ManualReviewStatus.Approved, review.Status);
    }

    [Fact]
    public void Approve_SetsApprovedByAndApprovedAt()
    {
        var review      = CompletedReview();
        var supervisorId = UserId.New();

        review.Approve(supervisorId);

        Assert.Equal(supervisorId, review.SupervisorId);
        Assert.NotNull(review.SupervisedAt);
    }

    [Fact]
    public void Reject_CompletedReview_ChangesStatusToRejected()
    {
        var review = CompletedReview();

        review.Reject("Answers look inconsistent", UserId.New());

        Assert.Equal(ManualReviewStatus.Rejected, review.Status);
    }

    [Fact]
    public void Reject_StoresReason()
    {
        var review = CompletedReview();

        review.Reject("OCR was actually readable", UserId.New());

        Assert.Equal("OCR was actually readable", review.RejectionReason);
    }

    [Fact]
    public void Approve_PendingReview_ThrowsInvalidOperation()
    {
        var ocrResult = OcrResult.Create(
            CaptureId.New(),
            [new ExtractedAnswer(1, "A", new OcrConfidence(0.3))]); // 0.3 < threshold → LowConfidence
        var review = ManualReview.CreateFor(ocrResult);

        Assert.Throws<InvalidOperationException>(() => review.Approve(UserId.New()));
    }

    [Fact]
    public void Reject_PendingReview_ThrowsInvalidOperation()
    {
        var ocrResult = OcrResult.Create(
            CaptureId.New(),
            [new ExtractedAnswer(1, "A", new OcrConfidence(0.3))]); // 0.3 < threshold → LowConfidence
        var review = ManualReview.CreateFor(ocrResult);

        Assert.Throws<InvalidOperationException>(() => review.Reject("Bad", UserId.New()));
    }

    [Fact]
    public void Approve_AlreadyApprovedReview_ThrowsInvalidOperation()
    {
        var review = CompletedReview();
        review.Approve(UserId.New());

        Assert.Throws<InvalidOperationException>(() => review.Approve(UserId.New()));
    }
}
