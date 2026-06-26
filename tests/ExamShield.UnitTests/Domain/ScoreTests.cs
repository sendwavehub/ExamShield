using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain;

public sealed class ScoreTests
{
    private static AnswerKey ThreeQuestionKey() =>
        new(new Dictionary<int, string> { [1] = "A", [2] = "B", [3] = "C" });

    private static ExtractedAnswer Answer(int q, string text) =>
        new(q, text, new OcrConfidence(0.95));

    [Fact]
    public void Create_WithAllCorrectAnswers_HasFullScore()
    {
        var answers = new[] { Answer(1, "A"), Answer(2, "B"), Answer(3, "C") };

        var score = Score.Create(CaptureId.New(), ExamId.New(), StudentId.New(), answers, ThreeQuestionKey());

        score.CorrectAnswers.Should().Be(3);
    }

    [Fact]
    public void Create_WithNoCorrectAnswers_HasZeroScore()
    {
        var answers = new[] { Answer(1, "X"), Answer(2, "X"), Answer(3, "X") };

        var score = Score.Create(CaptureId.New(), ExamId.New(), StudentId.New(), answers, ThreeQuestionKey());

        score.CorrectAnswers.Should().Be(0);
    }

    [Fact]
    public void Create_CountsOnlyCorrectAnswers()
    {
        var answers = new[] { Answer(1, "A"), Answer(2, "X"), Answer(3, "C") };

        var score = Score.Create(CaptureId.New(), ExamId.New(), StudentId.New(), answers, ThreeQuestionKey());

        score.CorrectAnswers.Should().Be(2);
    }

    [Fact]
    public void Create_CalculatesPercentage()
    {
        var answers = new[] { Answer(1, "A"), Answer(2, "X"), Answer(3, "C") };

        var score = Score.Create(CaptureId.New(), ExamId.New(), StudentId.New(), answers, ThreeQuestionKey());

        score.Percentage.Should().BeApproximately(66.67, 0.01);
    }

    [Fact]
    public void Create_StoresTotalQuestions()
    {
        var answers = new[] { Answer(1, "A"), Answer(2, "B"), Answer(3, "C") };

        var score = Score.Create(CaptureId.New(), ExamId.New(), StudentId.New(), answers, ThreeQuestionKey());

        score.TotalQuestions.Should().Be(3);
    }

    [Fact]
    public void Create_IsCaseInsensitiveForAnswerComparison()
    {
        var answers = new[] { Answer(1, "a"), Answer(2, "b"), Answer(3, "c") };

        var score = Score.Create(CaptureId.New(), ExamId.New(), StudentId.New(), answers, ThreeQuestionKey());

        score.CorrectAnswers.Should().Be(3);
    }
}
