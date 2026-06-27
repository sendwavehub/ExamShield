using ExamShield.Application.Queries.GetSettings;
using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Commands.UpdateSettings;

public sealed class UpdateSettingsCommandHandler(ISystemSettingsRepository repository)
    : IRequestHandler<UpdateSettingsCommand, SettingsDto>
{
    public async Task<SettingsDto> Handle(UpdateSettingsCommand request, CancellationToken ct)
    {
        var settings = await repository.GetAsync(ct);
        settings.Update(
            request.OcrConfidenceThreshold,
            request.NotificationsEnabled,
            request.NotificationSeverity,
            request.AccessTokenExpiryMinutes,
            request.RefreshTokenExpiryDays);
        await repository.SaveAsync(settings, ct);
        return new SettingsDto(
            settings.OcrConfidenceThreshold, settings.NotificationsEnabled,
            settings.NotificationSeverity, settings.AccessTokenExpiryMinutes,
            settings.RefreshTokenExpiryDays);
    }
}
