using ExamShield.Infrastructure.OCR;
using FluentAssertions;

namespace ExamShield.UnitTests.Infrastructure.OCR;

public sealed class StubOcrServiceTests
{
    private readonly StubOcrService _sut = new();

    [Fact]
    public async Task ExtractAsync_ReturnsThreeAnswers()
    {
        var result = await _sut.ExtractAsync(Array.Empty<byte>());
        result.Answers.Should().HaveCount(3);
    }

    [Fact]
    public async Task ExtractAsync_AnswersHaveExpectedQuestionNumbers()
    {
        var result = await _sut.ExtractAsync(Array.Empty<byte>());
        result.Answers.Select(a => a.QuestionNumber).Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public async Task ExtractAsync_AllAnswersHaveHighConfidence()
    {
        var result = await _sut.ExtractAsync(Array.Empty<byte>());
        result.Answers.Should().AllSatisfy(a => a.Confidence.Value.Should().BeGreaterThan(0.8));
    }

    [Fact]
    public async Task ExtractAsync_IgnoresImageContent()
    {
        var r1 = await _sut.ExtractAsync(new byte[] { 1, 2, 3 });
        var r2 = await _sut.ExtractAsync(new byte[] { 99, 100 });

        r1.Answers.Select(a => a.Text).Should().BeEquivalentTo(r2.Answers.Select(a => a.Text));
    }

    [Fact]
    public async Task ExtractAsync_CompletesWithoutException()
    {
        var act = async () => await _sut.ExtractAsync(Array.Empty<byte>());
        await act.Should().NotThrowAsync();
    }
}
