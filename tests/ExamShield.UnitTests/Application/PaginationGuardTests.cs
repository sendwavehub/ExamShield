using ExamShield.Application;
using FluentAssertions;

namespace ExamShield.UnitTests.Application;

public sealed class PaginationGuardTests
{
    [Fact]
    public void Validate_Page1AndSize1_DoesNotThrow()
    {
        var act = () => PaginationGuard.Validate(1, 1);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_MaxPageSize_DoesNotThrow()
    {
        var act = () => PaginationGuard.Validate(1, PaginationGuard.MaxPageSize);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_PageLessThan1_ThrowsArgumentOutOfRange(int page)
    {
        var act = () => PaginationGuard.Validate(page, 10);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PageSizeLessThan1_ThrowsArgumentOutOfRange(int pageSize)
    {
        var act = () => PaginationGuard.Validate(1, pageSize);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("pageSize");
    }

    [Fact]
    public void Validate_PageSizeExceedsMax_ThrowsArgumentOutOfRange()
    {
        var act = () => PaginationGuard.Validate(1, PaginationGuard.MaxPageSize + 1);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("pageSize");
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 50)]
    [InlineData(100, 200)]
    public void Validate_ValidCombinations_DoNotThrow(int page, int pageSize)
    {
        var act = () => PaginationGuard.Validate(page, pageSize);
        act.Should().NotThrow();
    }
}
