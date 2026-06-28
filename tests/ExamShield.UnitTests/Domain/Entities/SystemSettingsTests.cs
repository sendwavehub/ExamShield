using ExamShield.Domain.Entities;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class SystemSettingsTests
{
    [Fact]
    public void CreateDefault_ReturnsValidSettings()
    {
        var s = SystemSettings.CreateDefault();

        s.OcrConfidenceThreshold.Should().BeInRange(0.0, 1.0);
        s.AccessTokenExpiryMinutes.Should().BePositive();
        s.RefreshTokenExpiryDays.Should().BePositive();
        s.NotificationsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Update_ValidValues_UpdatesAllFields()
    {
        var s = SystemSettings.CreateDefault();
        s.Update(0.75, false, "Low", 30, 14);

        s.OcrConfidenceThreshold.Should().Be(0.75);
        s.NotificationsEnabled.Should().BeFalse();
        s.NotificationSeverity.Should().Be("Low");
        s.AccessTokenExpiryMinutes.Should().Be(30);
        s.RefreshTokenExpiryDays.Should().Be(14);
    }

    [Fact]
    public void Update_UpdatesUpdatedAt()
    {
        var s = SystemSettings.CreateDefault();
        var before = s.UpdatedAt;
        s.Update(0.9, true, "High", 60, 7);
        s.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Update_InvalidOcrThreshold_Throws(double threshold)
    {
        var s = SystemSettings.CreateDefault();
        var act = () => s.Update(threshold, true, "High", 60, 7);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_ZeroOrNegativeAccessTokenExpiry_Throws(int minutes)
    {
        var s = SystemSettings.CreateDefault();
        var act = () => s.Update(0.85, true, "High", minutes, 7);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_ZeroOrNegativeRefreshTokenExpiry_Throws(int days)
    {
        var s = SystemSettings.CreateDefault();
        var act = () => s.Update(0.85, true, "High", 60, days);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
