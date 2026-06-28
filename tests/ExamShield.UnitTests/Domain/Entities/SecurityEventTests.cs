using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class SecurityEventTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var evt = SecurityEvent.Create(
            SecurityEventType.LoginFailed, SecuritySeverity.Warning,
            "Invalid password", "user-id", "192.168.1.1");

        evt.EventType.Should().Be(SecurityEventType.LoginFailed);
        evt.Severity.Should().Be(SecuritySeverity.Warning);
        evt.Message.Should().Be("Invalid password");
        evt.UserId.Should().Be("user-id");
        evt.IpAddress.Should().Be("192.168.1.1");
        evt.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_OptionalFieldsNullByDefault()
    {
        var evt = SecurityEvent.Create(SecurityEventType.HashMismatch, SecuritySeverity.Critical, "Mismatch");
        evt.UserId.Should().BeNull();
        evt.IpAddress.Should().BeNull();
        evt.CaptureId.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyMessage_Throws(string message)
    {
        var act = () => SecurityEvent.Create(SecurityEventType.LoginFailed, SecuritySeverity.Info, message);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithCaptureId_SetsCaptureId()
    {
        var captureId = Guid.NewGuid();
        var evt = SecurityEvent.Create(
            SecurityEventType.HashMismatch, SecuritySeverity.Critical, "Mismatch",
            captureId: captureId);
        evt.CaptureId.Should().Be(captureId);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var a = SecurityEvent.Create(SecurityEventType.LoginSuccess, SecuritySeverity.Info, "Login");
        var b = SecurityEvent.Create(SecurityEventType.LoginSuccess, SecuritySeverity.Info, "Login");
        a.Id.Should().NotBe(b.Id);
    }
}
