using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class ScoreTests
{
    private static ExamId ExamId() => new(Guid.NewGuid());
    private static StudentId StudentId() => new(Guid.NewGuid());
    private static CaptureId CaptureId() => new(Guid.NewGuid());

    private static AnswerKey MakeKey(params (int q, string a)[] pairs) =>
        new(pairs.ToDictionary(p => p.q, p => p.a));

    private static ExtractedAnswer Ans(int q, string text) =>
        new(q, text, new OcrConfidence(0.9));

    [Fact]
    public void Create_AllCorrect_Returns100Percent()
    {
        var key = MakeKey((1, "A"), (2, "B"));
        var answers = new[] { Ans(1, "A"), Ans(2, "B") };

        var score = Score.Create(CaptureId(), ExamId(), StudentId(), answers, key);

        score.Percentage.Should().Be(100.0);
        score.CorrectAnswers.Should().Be(2);
        score.TotalQuestions.Should().Be(2);
    }

    [Fact]
    public void Create_AllWrong_Returns0Percent()
    {
        var key = MakeKey((1, "A"), (2, "B"));
        var answers = new[] { Ans(1, "C"), Ans(2, "D") };

        var score = Score.Create(CaptureId(), ExamId(), StudentId(), answers, key);

        score.Percentage.Should().Be(0.0);
        score.CorrectAnswers.Should().Be(0);
    }

    [Fact]
    public void Create_HalfCorrect_Returns50Percent()
    {
        var key = MakeKey((1, "A"), (2, "B"));
        var answers = new[] { Ans(1, "A"), Ans(2, "X") };

        var score = Score.Create(CaptureId(), ExamId(), StudentId(), answers, key);

        score.Percentage.Should().Be(50.0);
    }

    [Fact]
    public void Create_IsCaseInsensitive()
    {
        var key = MakeKey((1, "A"));
        var answers = new[] { Ans(1, "a") };

        var score = Score.Create(CaptureId(), ExamId(), StudentId(), answers, key);

        score.CorrectAnswers.Should().Be(1);
    }

    [Fact]
    public void Create_EmptyAnswerKey_Returns0Percent()
    {
        var key = new AnswerKey(new Dictionary<int, string>());
        var score = Score.Create(CaptureId(), ExamId(), StudentId(), [], key);

        score.Percentage.Should().Be(0.0);
        score.TotalQuestions.Should().Be(0);
    }

    [Fact]
    public void Create_SetsAllIdentifiers()
    {
        var captureId = CaptureId();
        var examId = ExamId();
        var studentId = StudentId();
        var key = MakeKey((1, "A"));

        var score = Score.Create(captureId, examId, studentId, [Ans(1, "A")], key);

        score.CaptureId.Should().Be(captureId);
        score.ExamId.Should().Be(examId);
        score.StudentId.Should().Be(studentId);
        score.Id.Should().NotBeNull();
    }

    [Fact]
    public void Create_NullCaptureId_Throws() =>
        FluentActions.Invoking(() =>
            Score.Create(null!, ExamId(), StudentId(), [], MakeKey()))
            .Should().Throw<ArgumentNullException>();

    [Fact]
    public void Publish_SetsIsPublished()
    {
        var score = Score.Create(CaptureId(), ExamId(), StudentId(), [], MakeKey());
        score.IsPublished.Should().BeFalse();

        score.Publish();

        score.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Publish_CalledTwice_IsIdempotent()
    {
        var score = Score.Create(CaptureId(), ExamId(), StudentId(), [], MakeKey());
        score.Publish();
        score.Publish(); // should not throw

        score.IsPublished.Should().BeTrue();
    }
}
