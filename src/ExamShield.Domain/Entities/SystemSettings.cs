namespace ExamShield.Domain.Entities;

public sealed class SystemSettings
{
    public int Id { get; private set; } = 1;
    public double OcrConfidenceThreshold { get; private set; } = 0.85;
    public bool NotificationsEnabled { get; private set; } = true;
    public string NotificationSeverity { get; private set; } = "High";
    public int AccessTokenExpiryMinutes { get; private set; } = 60;
    public int RefreshTokenExpiryDays { get; private set; } = 7;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private SystemSettings() { } // EF Core

    public static SystemSettings CreateDefault() => new();

    public void Update(
        double ocrConfidenceThreshold,
        bool notificationsEnabled,
        string notificationSeverity,
        int accessTokenExpiryMinutes,
        int refreshTokenExpiryDays)
    {
        OcrConfidenceThreshold = ocrConfidenceThreshold;
        NotificationsEnabled = notificationsEnabled;
        NotificationSeverity = notificationSeverity;
        AccessTokenExpiryMinutes = accessTokenExpiryMinutes;
        RefreshTokenExpiryDays = refreshTokenExpiryDays;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
