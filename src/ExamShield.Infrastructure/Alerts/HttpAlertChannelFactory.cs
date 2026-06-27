using ExamShield.Domain.Entities;

namespace ExamShield.Infrastructure.Alerts;

public sealed class HttpAlertChannelFactory(IHttpClientFactory httpClientFactory) : IAlertChannelFactory
{
    public IAlertChannel CreateChannel(string type, NotificationChannelSettings settings)
    {
        var client = httpClientFactory.CreateClient("Alerts");
        return type switch
        {
            "Slack"   => new SlackAlertChannel(client, settings.SlackWebhookUrl!),
            "Line"    => new LineNotifyAlertChannel(client, settings.LineNotifyToken!),
            "Webhook" => new WebhookAlertChannel(client, settings.WebhookUrl!),
            "Email"   => new EmailAlertChannel(
                             new NullSmtpEmailSender(),  // real SMTP config stays in appsettings
                             "noreply@examshield.local",
                             settings.EmailRecipients!),
            _ => new WebhookAlertChannel(client, settings.WebhookUrl ?? string.Empty)
        };
    }
}

// Used when SMTP isn't configured via appsettings — logs but doesn't crash.
file sealed class NullSmtpEmailSender : ISmtpEmailSender
{
    public Task SendAsync(string to, string from, string subject, string body, CancellationToken ct = default) =>
        Task.CompletedTask;
}
