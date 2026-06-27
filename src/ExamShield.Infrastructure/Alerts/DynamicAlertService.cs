using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;

namespace ExamShield.Infrastructure.Alerts;

/// <summary>Reads channel config from the database on every send so UI changes take effect immediately.</summary>
public sealed class DynamicAlertService(
    INotificationChannelSettingsRepository repo,
    IAlertChannelFactory factory)
{
    public async Task SendAsync(AlertType type, string message, CancellationToken ct = default)
    {
        var settings = await repo.GetAsync(ct);
        var channels = new List<IAlertChannel>();

        if (settings.EmailEnabled   && !string.IsNullOrWhiteSpace(settings.EmailRecipients))
            channels.Add(factory.CreateChannel("Email", settings));

        if (settings.SlackEnabled   && !string.IsNullOrWhiteSpace(settings.SlackWebhookUrl))
            channels.Add(factory.CreateChannel("Slack", settings));

        if (settings.LineEnabled    && !string.IsNullOrWhiteSpace(settings.LineNotifyToken))
            channels.Add(factory.CreateChannel("Line", settings));

        if (settings.WebhookEnabled && !string.IsNullOrWhiteSpace(settings.WebhookUrl))
            channels.Add(factory.CreateChannel("Webhook", settings));

        await Task.WhenAll(channels.Select(ch => ch.SendAsync(type, message, ct)));
    }
}
