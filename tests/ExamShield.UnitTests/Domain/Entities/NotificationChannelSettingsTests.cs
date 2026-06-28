using ExamShield.Domain.Entities;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class NotificationChannelSettingsTests
{
    [Fact]
    public void CreateDefault_AllChannelsDisabled()
    {
        var s = NotificationChannelSettings.CreateDefault();

        Assert.False(s.EmailEnabled);
        Assert.False(s.SlackEnabled);
        Assert.False(s.LineEnabled);
        Assert.False(s.WebhookEnabled);
    }

    [Fact]
    public void Update_StoresAllFields()
    {
        var s = NotificationChannelSettings.CreateDefault();

        s.Update(
            emailEnabled: true,    emailRecipients: "a@b.com,c@d.com",
            slackEnabled: true,    slackWebhookUrl: "https://hooks.slack.com/T/X",
            lineEnabled: false,    lineNotifyToken: null,
            webhookEnabled: true,  webhookUrl: "https://my.api/notify");

        Assert.True(s.EmailEnabled);
        Assert.Equal("a@b.com,c@d.com", s.EmailRecipients);
        Assert.True(s.SlackEnabled);
        Assert.Equal("https://hooks.slack.com/T/X", s.SlackWebhookUrl);
        Assert.False(s.LineEnabled);
        Assert.True(s.WebhookEnabled);
        Assert.Equal("https://my.api/notify", s.WebhookUrl);
    }

    [Fact]
    public void Update_SlackEnabledWithoutUrl_ThrowsArgumentException()
    {
        var s = NotificationChannelSettings.CreateDefault();

        Assert.Throws<ArgumentException>(() =>
            s.Update(false, null, slackEnabled: true, slackWebhookUrl: null, false, null, false, null));
    }

    [Fact]
    public void Update_WebhookEnabledWithoutUrl_ThrowsArgumentException()
    {
        var s = NotificationChannelSettings.CreateDefault();

        Assert.Throws<ArgumentException>(() =>
            s.Update(false, null, false, null, false, null, webhookEnabled: true, webhookUrl: null));
    }

    [Fact]
    public void Update_EmailEnabledWithoutRecipients_ThrowsArgumentException()
    {
        var s = NotificationChannelSettings.CreateDefault();

        Assert.Throws<ArgumentException>(() =>
            s.Update(emailEnabled: true, emailRecipients: null, false, null, false, null, false, null));
    }

    [Fact]
    public void Update_InvalidSlackUrl_ThrowsArgumentException()
    {
        var s = NotificationChannelSettings.CreateDefault();

        Assert.Throws<ArgumentException>(() =>
            s.Update(false, null, slackEnabled: true, slackWebhookUrl: "not-a-url", false, null, false, null));
    }

    [Fact]
    public void Update_InvalidWebhookUrl_ThrowsArgumentException()
    {
        var s = NotificationChannelSettings.CreateDefault();

        Assert.Throws<ArgumentException>(() =>
            s.Update(false, null, false, null, false, null, webhookEnabled: true, webhookUrl: "ftp://bad-scheme"));
    }

    [Fact]
    public void Update_StampsUpdatedAt()
    {
        var s = NotificationChannelSettings.CreateDefault();
        var before = DateTimeOffset.UtcNow;

        s.Update(false, null, false, null, false, null, false, null);

        Assert.True(s.UpdatedAt >= before);
    }

    [Fact]
    public void Update_LineEnabledWithToken_StoresToken()
    {
        var s = NotificationChannelSettings.CreateDefault();

        s.Update(false, null, false, null,
                 lineEnabled: true, lineNotifyToken: "line-secret-token",
                 false, null);

        Assert.True(s.LineEnabled);
        Assert.Equal("line-secret-token", s.LineNotifyToken);
    }

    [Fact]
    public void Update_AllChannelsDisabled_AcceptsNullUrls()
    {
        var s = NotificationChannelSettings.CreateDefault();

        var act = () => s.Update(false, null, false, null, false, null, false, null);

        // Should not throw — disabled channels don't require URL values
        act();
        Assert.False(s.EmailEnabled);
        Assert.False(s.SlackEnabled);
        Assert.False(s.LineEnabled);
        Assert.False(s.WebhookEnabled);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_EmailEnabledWithWhitespaceRecipients_ThrowsArgumentException(string recipients)
    {
        var s = NotificationChannelSettings.CreateDefault();

        Assert.Throws<ArgumentException>(() =>
            s.Update(emailEnabled: true, emailRecipients: recipients, false, null, false, null, false, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_SlackEnabledWithWhitespaceUrl_ThrowsArgumentException(string url)
    {
        var s = NotificationChannelSettings.CreateDefault();

        Assert.Throws<ArgumentException>(() =>
            s.Update(false, null, slackEnabled: true, slackWebhookUrl: url, false, null, false, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WebhookEnabledWithWhitespaceUrl_ThrowsArgumentException(string url)
    {
        var s = NotificationChannelSettings.CreateDefault();

        Assert.Throws<ArgumentException>(() =>
            s.Update(false, null, false, null, false, null, webhookEnabled: true, webhookUrl: url));
    }

    [Fact]
    public void CreateDefault_IdIsOne()
    {
        var s = NotificationChannelSettings.CreateDefault();

        Assert.Equal(1, s.Id);
    }
}
