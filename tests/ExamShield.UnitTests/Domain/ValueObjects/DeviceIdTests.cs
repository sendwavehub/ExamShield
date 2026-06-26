using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.ValueObjects;

public sealed class DeviceIdTests
{
    [Fact]
    public void Create_WithEmptyGuid_ThrowsArgumentException()
    {
        var act = () => new DeviceId(Guid.Empty);

        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void Create_WithValidGuid_Succeeds()
    {
        var guid = Guid.NewGuid();

        new DeviceId(guid).Value.Should().Be(guid);
    }

    [Fact]
    public void TwoInstances_WithSameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();

        new DeviceId(guid).Should().Be(new DeviceId(guid));
    }

    [Fact]
    public void New_GeneratesUniqueIds()
    {
        DeviceId.New().Should().NotBe(DeviceId.New());
    }
}
