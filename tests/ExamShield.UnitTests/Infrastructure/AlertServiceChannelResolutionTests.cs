using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Infrastructure.Alerts;
using NSubstitute;

// IAlertChannelFactory and DynamicAlertService live in ExamShield.Infrastructure.Alerts (same namespace as above)

namespace ExamShield.UnitTests.Infrastructure;

public sealed class AlertServiceChannelResolutionTests
{
    private static INotificationChannelSettingsRepository RepoWith(Action<NotificationChannelSettings> configure)
    {
        var settings = NotificationChannelSettings.CreateDefault();
        configure(settings);
        var repo = Substitute.For<INotificationChannelSettingsRepository>();
        repo.GetAsync(default).ReturnsForAnyArgs(Task.FromResult(settings));
        return repo;
    }

    [Fact]
    public async Task SendAsync_AllChannelsDisabled_SendsToNoChannels()
    {
        var tracker = new ChannelCallTracker();
        var repo    = RepoWith(_ => { }); // all disabled by default
        var svc     = new DynamicAlertService(repo, tracker);

        await svc.SendAsync(AlertType.HashMismatch, "test");

        Assert.Equal(0, tracker.CallCount);
    }

    [Fact]
    public async Task SendAsync_SlackEnabled_SendsToSlack()
    {
        var tracker = new ChannelCallTracker();
        var repo    = RepoWith(s => s.Update(
            false, null, slackEnabled: true, slackWebhookUrl: "https://hooks.slack.com/T/X",
            false, null, false, null));
        var svc = new DynamicAlertService(repo, tracker);

        await svc.SendAsync(AlertType.HashMismatch, "tamper detected");

        Assert.Equal(1, tracker.CallCount);
        Assert.Contains("slack", tracker.LastChannelType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendAsync_MultipleChannelsEnabled_SendsToAll()
    {
        var tracker = new ChannelCallTracker();
        var repo    = RepoWith(s => s.Update(
            emailEnabled: true,  emailRecipients: "a@b.com",
            slackEnabled: true,  slackWebhookUrl: "https://hooks.slack.com/T/X",
            lineEnabled:  false, lineNotifyToken: null,
            webhookEnabled: false, webhookUrl: null));
        var svc = new DynamicAlertService(repo, tracker);

        await svc.SendAsync(AlertType.InvalidSignature, "bad sig");

        Assert.Equal(2, tracker.CallCount);
    }
}

/// <summary>Tracks channel invocations without making real HTTP calls.</summary>
public sealed class ChannelCallTracker : IAlertChannelFactory
{
    private int _callCount;
    private string _lastChannelType = string.Empty;

    public int CallCount => _callCount;
    public string LastChannelType => _lastChannelType;

    public IAlertChannel CreateChannel(string type, NotificationChannelSettings settings) =>
        new TrackingChannel(type, () => { _callCount++; _lastChannelType = type; });
}

public sealed class TrackingChannel(string type, Action onSend) : IAlertChannel
{
    public string Type => type;
    public Task SendAsync(AlertType alertType, string message, CancellationToken ct = default)
    {
        onSend();
        return Task.CompletedTask;
    }
}
