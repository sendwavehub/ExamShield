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
}
