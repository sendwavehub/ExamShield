using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.ValueObjects;

public sealed class CaptureIdTests
{
    [Fact]
    public void Constructor_ValidGuid_SetsValue()
    {
        var id = Guid.NewGuid();
        var captureId = new CaptureId(id);
        captureId.Value.Should().Be(id);
    }

    [Fact]
    public void Constructor_EmptyGuid_Throws()
    {
        var act = () => new CaptureId(Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void New_GeneratesUniqueIds()
    {
        var a = CaptureId.New();
        var b = CaptureId.New();
        a.Value.Should().NotBe(b.Value);
    }

    [Fact]
    public void Equals_SameGuid_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        new CaptureId(id).Should().Be(new CaptureId(id));
    }

    [Fact]
    public void Equals_DifferentGuid_ReturnsFalse()
    {
        new CaptureId(Guid.NewGuid()).Should().NotBe(new CaptureId(Guid.NewGuid()));
    }
}
