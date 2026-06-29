using ExamShield.Infrastructure.Messaging;
using FluentAssertions;

namespace ExamShield.UnitTests.Infrastructure.Messaging;

public sealed class OcrRetryPolicyTests
{
    [Fact]
    public void GetRetryCount_NullHeaders_ReturnsZero()
    {
        OcrRetryPolicy.GetRetryCount(null).Should().Be(0);
    }

    [Fact]
    public void GetRetryCount_EmptyHeaders_ReturnsZero()
    {
        OcrRetryPolicy.GetRetryCount(new Dictionary<string, object?>()).Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GetRetryCount_IntHeader_ReturnsValue(int count)
    {
        var headers = new Dictionary<string, object?> { ["x-retry-count"] = count };
        OcrRetryPolicy.GetRetryCount(headers).Should().Be(count);
    }

    [Fact]
    public void GetRetryCount_LongHeader_ReturnsIntValue()
    {
        var headers = new Dictionary<string, object?> { ["x-retry-count"] = 2L };
        OcrRetryPolicy.GetRetryCount(headers).Should().Be(2);
    }

    [Theory]
    [InlineData(0, 3, true)]   // first attempt, should retry
    [InlineData(1, 3, true)]   // second attempt, should retry
    [InlineData(2, 3, false)]  // third attempt, exhausted → DLQ
    [InlineData(0, 1, false)]  // maxRetries=1 means no retries at all
    public void ShouldRetry_VariousAttempts_MatchesExpected(int currentCount, int maxRetries, bool expected)
    {
        var headers = new Dictionary<string, object?> { ["x-retry-count"] = currentCount };
        OcrRetryPolicy.ShouldRetry(headers, maxRetries).Should().Be(expected);
    }

    [Fact]
    public void ShouldRetry_NullHeaders_TreatsAsFirstAttempt()
    {
        OcrRetryPolicy.ShouldRetry(null, maxRetries: 3).Should().BeTrue();
    }
}
