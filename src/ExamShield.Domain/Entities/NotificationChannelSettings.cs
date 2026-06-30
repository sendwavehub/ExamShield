using System.Net;
using System.Net.Sockets;

namespace ExamShield.Domain.Entities;

public sealed class NotificationChannelSettings
{
    public int Id { get; private set; } = 1;

    public bool EmailEnabled { get; private set; }
    public string? EmailRecipients { get; private set; }

    public bool SlackEnabled { get; private set; }
    public string? SlackWebhookUrl { get; private set; }

    public bool LineEnabled { get; private set; }
    public string? LineNotifyToken { get; private set; }

    public bool WebhookEnabled { get; private set; }
    public string? WebhookUrl { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private NotificationChannelSettings() { }

    public static NotificationChannelSettings CreateDefault() => new();

    public void Update(
        bool emailEnabled,   string? emailRecipients,
        bool slackEnabled,   string? slackWebhookUrl,
        bool lineEnabled,    string? lineNotifyToken,
        bool webhookEnabled, string? webhookUrl)
    {
        if (emailEnabled && string.IsNullOrWhiteSpace(emailRecipients))
            throw new ArgumentException("Email recipients are required when email notifications are enabled.", nameof(emailRecipients));

        if (slackEnabled)
        {
            if (string.IsNullOrWhiteSpace(slackWebhookUrl))
                throw new ArgumentException("Slack webhook URL is required when Slack notifications are enabled.", nameof(slackWebhookUrl));
            if (!IsSafeWebhookUrl(slackWebhookUrl))
                throw new ArgumentException("Slack webhook URL must be a public HTTPS URL (localhost and private IP ranges are not permitted).", nameof(slackWebhookUrl));
        }

        if (webhookEnabled)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
                throw new ArgumentException("Webhook URL is required when webhook notifications are enabled.", nameof(webhookUrl));
            if (!IsSafeWebhookUrl(webhookUrl))
                throw new ArgumentException("Webhook URL must be a public HTTPS URL (localhost and private IP ranges are not permitted).", nameof(webhookUrl));
        }

        EmailEnabled   = emailEnabled;   EmailRecipients  = emailRecipients;
        SlackEnabled   = slackEnabled;   SlackWebhookUrl  = slackWebhookUrl;
        LineEnabled    = lineEnabled;    LineNotifyToken  = lineNotifyToken;
        WebhookEnabled = webhookEnabled; WebhookUrl       = webhookUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static bool IsSafeWebhookUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;
        if (uri.Scheme != Uri.UriSchemeHttps)
            return false;
        return !IsPrivateOrLoopbackHost(uri.Host);
    }

    private static bool IsPrivateOrLoopbackHost(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            return true;
        if (!IPAddress.TryParse(host, out var ip))
            return false;
        if (IPAddress.IsLoopback(ip))
            return true;
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();
        if (ip.AddressFamily != AddressFamily.InterNetwork)
            return false;
        var b = ip.GetAddressBytes();
        return b[0] == 10
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            || (b[0] == 192 && b[1] == 168)
            || (b[0] == 169 && b[1] == 254);
    }
}
