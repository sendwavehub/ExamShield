using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.ValueObjects;

public sealed class StudentIdTests
{
    [Fact]
    public void Create_WithEmptyGuid_ThrowsArgumentException()
    {
        var act = () => new StudentId(Guid.Empty);

        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void Create_WithValidGuid_Succeeds()
    {
        var guid = Guid.NewGuid();

        new StudentId(guid).Value.Should().Be(guid);
    }

    [Fact]
    public void TwoInstances_WithSameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();

        new StudentId(guid).Should().Be(new StudentId(guid));
    }

    [Fact]
    public void New_GeneratesUniqueIds()
    {
        StudentId.New().Should().NotBe(StudentId.New());
    }
}
