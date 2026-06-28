using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class OcrResultTests
{
    private static CaptureId CaptureId() => new(Guid.NewGuid());

    private static ExtractedAnswer Ans(int q, double confidence) =>
        new(q, "A", new OcrConfidence(confidence));

    [Fact]
    public void Create_AllHighConfidence_StatusIsCompleted()
    {
        var result = OcrResult.Create(CaptureId(), [Ans(1, 0.95), Ans(2, 0.90)]);
        result.Status.Should().Be(OcrStatus.Completed);
        result.RequiresManualReview.Should().BeFalse();
    }

    [Fact]
    public void Create_AnyLowConfidence_StatusIsLowConfidence()
    {
        var result = OcrResult.Create(CaptureId(), [Ans(1, 0.95), Ans(2, 0.3)]);
        result.Status.Should().Be(OcrStatus.LowConfidence);
        result.RequiresManualReview.Should().BeTrue();
    }

    [Fact]
    public void Create_AnswersAtThreshold_StatusIsCompleted()
    {
        var result = OcrResult.Create(CaptureId(), [Ans(1, OcrConfidence.LowThreshold)]);
        result.Status.Should().Be(OcrStatus.Completed);
    }

    [Fact]
    public void Create_ComputesAverageConfidence()
    {
        var result = OcrResult.Create(CaptureId(), [Ans(1, 0.9), Ans(2, 0.8)]);
        result.OverallConfidence.Value.Should().BeApproximately(0.85, 0.001);
    }

    [Fact]
    public void Create_EmptyAnswers_OverallConfidenceIsZero()
    {
        var result = OcrResult.Create(CaptureId(), []);
        result.OverallConfidence.Value.Should().Be(0.0);
    }

    [Fact]
    public void Create_SetsIdAndCaptureIdAndTimestamp()
    {
        var captureId = CaptureId();
        var result = OcrResult.Create(captureId, [Ans(1, 0.9)]);

        result.Id.Should().NotBeNull();
        result.CaptureId.Should().Be(captureId);
        result.ProcessedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_NullAnswers_Throws()
    {
        var act = () => OcrResult.Create(CaptureId(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_CustomThreshold_AppliesToStatusDecision()
    {
        // confidence 0.7 is above 0.6 threshold but below default 0.8
        var result = OcrResult.Create(CaptureId(), [Ans(1, 0.7)], confidenceThreshold: 0.6);
        result.Status.Should().Be(OcrStatus.Completed);
    }
}
