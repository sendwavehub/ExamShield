using MediatR;

namespace ExamShield.Application.Queries.GetSettings;

public sealed record SettingsDto(
    double OcrConfidenceThreshold,
    bool NotificationsEnabled,
    string NotificationSeverity,
    int AccessTokenExpiryMinutes,
    int RefreshTokenExpiryDays);

public sealed record GetSettingsQuery : IRequest<SettingsDto>;
