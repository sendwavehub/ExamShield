using ExamShield.Domain.Entities;

namespace ExamShield.UnitTests.Domain;

public sealed class SystemSettingsTests
{
    private static SystemSettings Default() => SystemSettings.CreateDefault();

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Update_InvalidThreshold_Throws(double threshold)
    {
        var s = Default();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            s.Update(threshold, true, "High", 60, 7));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(0.85)]
    [InlineData(1.0)]
    public void Update_ValidThreshold_Persists(double threshold)
    {
        var s = Default();
        s.Update(threshold, true, "High", 60, 7);
        Assert.Equal(threshold, s.OcrConfidenceThreshold);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Update_InvalidAccessTokenExpiry_Throws(int minutes)
    {
        var s = Default();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            s.Update(0.85, true, "High", minutes, 7));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_InvalidRefreshTokenExpiry_Throws(int days)
    {
        var s = Default();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            s.Update(0.85, true, "High", 60, days));
    }
}
