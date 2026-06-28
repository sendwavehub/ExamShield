using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.ValueObjects;

public sealed class AnswerKeyTests
{
    private static AnswerKey Make(params (int q, string a)[] pairs) =>
        new(pairs.ToDictionary(p => p.q, p => p.a));

    [Fact]
    public void IsCorrect_MatchingAnswer_ReturnsTrue()
    {
        var key = Make((1, "A"), (2, "B"));
        key.IsCorrect(1, "A").Should().BeTrue();
    }

    [Fact]
    public void IsCorrect_WrongAnswer_ReturnsFalse()
    {
        var key = Make((1, "A"));
        key.IsCorrect(1, "B").Should().BeFalse();
    }

    [Fact]
    public void IsCorrect_IsCaseInsensitive()
    {
        var key = Make((1, "A"));
        key.IsCorrect(1, "a").Should().BeTrue();
    }

    [Fact]
    public void IsCorrect_QuestionNotInKey_ReturnsFalse()
    {
        var key = Make((1, "A"));
        key.IsCorrect(99, "A").Should().BeFalse();
    }

    [Fact]
    public void Count_ReturnsNumberOfQuestions()
    {
        var key = Make((1, "A"), (2, "B"), (3, "C"));
        key.Count.Should().Be(3);
    }

    [Fact]
    public void Constructor_NullAnswers_ThrowsArgumentNullException()
    {
        var act = () => new AnswerKey(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Count_EmptyKey_ReturnsZero()
    {
        var key = new AnswerKey(new Dictionary<int, string>());
        key.Count.Should().Be(0);
    }
}
