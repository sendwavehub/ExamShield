using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.ValueObjects;

public sealed class ExtractedAnswerTests
{
    private static OcrConfidence High() => new(0.95);

    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var answer = new ExtractedAnswer(3, "B", High());
        answer.QuestionNumber.Should().Be(3);
        answer.Text.Should().Be("B");
        answer.Confidence.Value.Should().Be(0.95);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ZeroOrNegativeQuestion_Throws(int q)
    {
        var act = () => new ExtractedAnswer(q, "A", High());
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyText_Throws(string text)
    {
        var act = () => new ExtractedAnswer(1, text, High());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NullConfidence_Throws()
    {
        var act = () => new ExtractedAnswer(1, "A", null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
