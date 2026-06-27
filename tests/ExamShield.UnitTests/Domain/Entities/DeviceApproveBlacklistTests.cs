using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class DeviceApproveBlacklistTests
{
    private static Device MakeDevice() =>
        Device.Register("Scanner-1", new PublicKey(new byte[32]));

    [Fact]
    public void Approve_BlacklistedDevice_ThrowsInvalidOperationException()
    {
        var device = MakeDevice();
        device.Approve();
        device.Blacklist("stolen");

        var act = () => device.Approve();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*blacklisted*");
    }

    [Fact]
    public void Approve_PendingDevice_Succeeds()
    {
        var device = MakeDevice();

        device.Approve();

        device.Status.Should().Be(DeviceStatus.Approved);
    }

    [Fact]
    public void Approve_DisabledDevice_Succeeds()
    {
        var device = MakeDevice();
        device.Approve();
        device.Disable();

        device.Approve();

        device.Status.Should().Be(DeviceStatus.Approved);
    }
}
