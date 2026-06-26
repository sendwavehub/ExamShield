using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain;

public sealed class OcrResultTests
{
    private static ExtractedAnswer HighConfidenceAnswer(int q = 1) =>
        new(q, "A", new OcrConfidence(0.95));

    private static ExtractedAnswer LowConfidenceAnswer(int q = 1) =>
        new(q, "B", new OcrConfidence(0.50));

    [Fact]
    public void Create_WithAllHighConfidenceAnswers_HasCompletedStatus()
    {
        var answers = new[] { HighConfidenceAnswer(1), HighConfidenceAnswer(2) };

        var result = OcrResult.Create(CaptureId.New(), answers);

        result.Status.Should().Be(OcrStatus.Completed);
    }

    [Fact]
    public void Create_WithAnyLowConfidenceAnswer_HasLowConfidenceStatus()
    {
        var answers = new[] { HighConfidenceAnswer(1), LowConfidenceAnswer(2) };

        var result = OcrResult.Create(CaptureId.New(), answers);

        result.Status.Should().Be(OcrStatus.LowConfidence);
    }

    [Fact]
    public void Create_WithLowConfidenceStatus_RequiresManualReview()
    {
        var answers = new[] { LowConfidenceAnswer(1) };

        var result = OcrResult.Create(CaptureId.New(), answers);

        result.RequiresManualReview.Should().BeTrue();
    }

    [Fact]
    public void Create_WithAllHighConfidence_DoesNotRequireManualReview()
    {
        var answers = new[] { HighConfidenceAnswer(1), HighConfidenceAnswer(2) };

        var result = OcrResult.Create(CaptureId.New(), answers);

        result.RequiresManualReview.Should().BeFalse();
    }

    [Fact]
    public void Create_StoresAllAnswers()
    {
        var answers = new[] { HighConfidenceAnswer(1), HighConfidenceAnswer(2) };

        var result = OcrResult.Create(CaptureId.New(), answers);

        result.Answers.Should().HaveCount(2);
    }
}
