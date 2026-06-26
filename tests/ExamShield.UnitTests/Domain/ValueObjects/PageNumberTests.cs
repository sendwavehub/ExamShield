using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.ValueObjects;

public sealed class PageNumberTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithNonPositiveValue_ThrowsArgumentException(int value)
    {
        var act = () => new PageNumber(value);

        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void Create_WithPositiveValue_Succeeds()
    {
        new PageNumber(1).Value.Should().Be(1);
        new PageNumber(99).Value.Should().Be(99);
    }

    [Fact]
    public void TwoInstances_WithSameValue_AreEqual()
    {
        new PageNumber(3).Should().Be(new PageNumber(3));
    }
}
