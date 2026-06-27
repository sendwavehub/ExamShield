using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class DeviceBlacklistTests
{
    private static Device PendingDevice() =>
        Device.Register("Test", new PublicKey(new byte[64]));

    [Fact]
    public void Blacklist_PendingDevice_SetsBlacklistedStatus()
    {
        var device = PendingDevice();
        device.Blacklist("stolen");
        Assert.Equal(DeviceStatus.Blacklisted, device.Status);
    }

    [Fact]
    public void Blacklist_ApprovedDevice_SetsBlacklistedStatus()
    {
        var device = PendingDevice();
        device.Approve();
        device.Blacklist("compromised key");
        Assert.Equal(DeviceStatus.Blacklisted, device.Status);
    }

    [Fact]
    public void Blacklist_StoresReason()
    {
        var device = PendingDevice();
        device.Blacklist("hardware stolen");
        Assert.Equal("hardware stolen", device.BlacklistReason);
    }

    [Fact]
    public void Blacklist_EmptyReason_Throws()
    {
        var device = PendingDevice();
        Assert.Throws<ArgumentException>(() => device.Blacklist("  "));
    }

    [Fact]
    public void Blacklist_AlreadyBlacklisted_Throws()
    {
        var device = PendingDevice();
        device.Blacklist("reason 1");
        Assert.Throws<InvalidOperationException>(() => device.Blacklist("reason 2"));
    }

    [Fact]
    public void Enable_BlacklistedDevice_Throws()
    {
        var device = PendingDevice();
        device.Approve();
        device.Blacklist("compromised");
        Assert.Throws<InvalidOperationException>(() => device.Enable());
    }

    [Fact]
    public void IsActive_BlacklistedDevice_ReturnsFalse()
    {
        var device = PendingDevice();
        device.Approve();
        device.Blacklist("stolen");
        Assert.False(device.IsActive);
    }
}
