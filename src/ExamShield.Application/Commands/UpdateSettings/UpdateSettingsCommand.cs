using ExamShield.Application.Queries.GetSettings;
using MediatR;

namespace ExamShield.Application.Commands.UpdateSettings;

public sealed record UpdateSettingsCommand(
    double OcrConfidenceThreshold,
    bool NotificationsEnabled,
    string NotificationSeverity,
    int AccessTokenExpiryMinutes,
    int RefreshTokenExpiryDays) : IRequest<SettingsDto>;
