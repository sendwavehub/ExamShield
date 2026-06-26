using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Entities;

public sealed class ManualReview : AggregateRoot
{
    public ManualReviewId Id { get; private set; } = null!;
    public OcrResultId OcrResultId { get; private set; } = null!;
    public CaptureId CaptureId { get; private set; } = null!;
    public ManualReviewStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<ReviewedAnswer> ReviewedAnswers { get; private set; } = [];
    public UserId? ReviewedBy { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private ManualReview() { }

    public static ManualReview CreateFor(OcrResult ocrResult)
    {
        ArgumentNullException.ThrowIfNull(ocrResult);
        return new ManualReview
        {
            Id = ManualReviewId.New(),
            OcrResultId = ocrResult.Id,
            CaptureId = ocrResult.CaptureId,
            Status = ManualReviewStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Complete(IReadOnlyList<ReviewedAnswer> answers, UserId reviewedBy)
    {
        if (Status != ManualReviewStatus.Pending)
            throw new InvalidOperationException("Only a pending review can be completed.");
        if (answers is null || answers.Count == 0)
            throw new ArgumentException("At least one reviewed answer is required.", nameof(answers));
        ArgumentNullException.ThrowIfNull(reviewedBy);

        ReviewedAnswers = answers.ToList();
        ReviewedBy = reviewedBy;
        Status = ManualReviewStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
