using ExamShield.Domain.Entities;
using ExamShield.Infrastructure.Alerts;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Infrastructure.Alerts;

public sealed class HttpAlertChannelFactoryTests
{
    private static IHttpClientFactory BuildFactory()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());
        return factory;
    }

    private static NotificationChannelSettings Settings() =>
        NotificationChannelSettings.CreateDefault();

    [Theory]
    [InlineData("Slack")]
    [InlineData("Line")]
    [InlineData("Webhook")]
    [InlineData("Email")]
    public void CreateChannel_KnownType_ReturnsNonNullChannel(string type)
    {
        var sut = new HttpAlertChannelFactory(BuildFactory());
        var settings = NotificationChannelSettings.CreateDefault();
        settings.Update(
            emailEnabled: true, emailRecipients: "a@b.com",
            slackEnabled: true, slackWebhookUrl: "https://hooks.slack.com/t/x",
            lineEnabled: true, lineNotifyToken: "line-token",
            webhookEnabled: true, webhookUrl: "https://example.com/hook");

        var channel = sut.CreateChannel(type, settings);

        channel.Should().NotBeNull();
    }

    [Fact]
    public void CreateChannel_UnknownType_FallsBackToWebhook()
    {
        var sut = new HttpAlertChannelFactory(BuildFactory());
        var settings = Settings();
        settings.Update(false, null, false, null, false, null, true, "https://example.com/hook");

        var channel = sut.CreateChannel("Unknown", settings);

        channel.Should().BeOfType<WebhookAlertChannel>();
    }

    [Fact]
    public void CreateChannel_SlackType_ReturnsSlackChannel()
    {
        var sut = new HttpAlertChannelFactory(BuildFactory());
        var settings = Settings();
        settings.Update(false, null, true, "https://hooks.slack.com/t/x", false, null, false, null);

        var channel = sut.CreateChannel("Slack", settings);

        channel.Should().BeOfType<SlackAlertChannel>();
    }

    [Fact]
    public void CreateChannel_LineType_ReturnsLineChannel()
    {
        var sut = new HttpAlertChannelFactory(BuildFactory());
        var settings = Settings();
        settings.Update(false, null, false, null, true, "tok", false, null);

        var channel = sut.CreateChannel("Line", settings);

        channel.Should().BeOfType<LineNotifyAlertChannel>();
    }

    [Fact]
    public void CreateChannel_EmailType_ReturnsEmailChannel()
    {
        var sut = new HttpAlertChannelFactory(BuildFactory());
        var settings = Settings();
        settings.Update(true, "a@b.com", false, null, false, null, false, null);

        var channel = sut.CreateChannel("Email", settings);

        channel.Should().BeOfType<EmailAlertChannel>();
    }
}
