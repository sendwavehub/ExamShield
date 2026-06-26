using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExamShield.Infrastructure.Alerts;

public sealed class AlertService : IAlertService
{
    private readonly IReadOnlyList<IAlertChannel> _channels;

    public AlertService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        var configs = configuration
            .GetSection("Alerts:Channels")
            .Get<AlertChannelConfig[]>() ?? [];

        _channels = configs
            .Where(c => c.Enabled && !string.IsNullOrWhiteSpace(c.Url))
            .Select(c => CreateChannel(c, httpClientFactory))
            .ToList();
    }

    public Task SendAsync(AlertType type, string message, CancellationToken ct = default) =>
        Task.WhenAll(_channels.Select(ch => ch.SendAsync(type, message, ct)));

    private static IAlertChannel CreateChannel(AlertChannelConfig config, IHttpClientFactory factory)
    {
        var client = factory.CreateClient("Alerts");
        return config.Type switch
        {
            "Slack" => new SlackAlertChannel(client, config.Url),
            _ => new WebhookAlertChannel(client, config.Url)  // "Webhook" + default
        };
    }
}

public sealed class AlertChannelConfig
{
    public string Type { get; set; } = "Webhook";
    public string Url { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
