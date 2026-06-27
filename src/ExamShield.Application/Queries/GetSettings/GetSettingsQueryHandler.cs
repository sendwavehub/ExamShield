using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Queries.GetSettings;

public sealed class GetSettingsQueryHandler(ISystemSettingsRepository repository)
    : IRequestHandler<GetSettingsQuery, SettingsDto>
{
    public async Task<SettingsDto> Handle(GetSettingsQuery request, CancellationToken ct)
    {
        var s = await repository.GetAsync(ct);
        return new SettingsDto(
            s.OcrConfidenceThreshold, s.NotificationsEnabled,
            s.NotificationSeverity, s.AccessTokenExpiryMinutes, s.RefreshTokenExpiryDays);
    }
}
