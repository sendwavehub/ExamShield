using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.ValueObjects;

public sealed class ExamIdTests
{
    [Fact]
    public void Create_WithEmptyGuid_ThrowsArgumentException()
    {
        var act = () => new ExamId(Guid.Empty);

        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void Create_WithValidGuid_Succeeds()
    {
        var guid = Guid.NewGuid();

        var id = new ExamId(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void TwoInstances_WithSameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();

        new ExamId(guid).Should().Be(new ExamId(guid));
    }

    [Fact]
    public void TwoInstances_WithDifferentGuids_AreNotEqual()
    {
        new ExamId(Guid.NewGuid()).Should().NotBe(new ExamId(Guid.NewGuid()));
    }

    [Fact]
    public void New_GeneratesUniqueIds()
    {
        ExamId.New().Should().NotBe(ExamId.New());
    }
}
