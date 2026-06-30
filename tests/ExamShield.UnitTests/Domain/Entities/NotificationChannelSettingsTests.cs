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

    // ── SSRF protection — webhook URLs ───────────────────────────────────────

    [Theory]
    [InlineData("http://example.com/alert")]          // plain HTTP not allowed
    [InlineData("https://localhost/alert")]            // localhost
    [InlineData("https://127.0.0.1/alert")]           // loopback
    [InlineData("https://10.0.0.1/alert")]            // RFC-1918 class A
    [InlineData("https://172.16.0.1/alert")]          // RFC-1918 class B
    [InlineData("https://172.31.255.255/alert")]      // RFC-1918 class B upper bound
    [InlineData("https://192.168.1.100/alert")]       // RFC-1918 class C
    [InlineData("https://169.254.169.254/latest")]    // AWS IMDS link-local
    public void Update_WebhookUrlWithSsrfTarget_ThrowsArgumentException(string url)
    {
        var s = NotificationChannelSettings.CreateDefault();

        Assert.Throws<ArgumentException>(() =>
            s.Update(false, null, false, null, false, null, webhookEnabled: true, webhookUrl: url));
    }

    [Theory]
    [InlineData("http://hooks.slack.com/T/XYZ")]      // plain HTTP
    [InlineData("https://localhost/hook")]             // localhost
    [InlineData("https://127.0.0.1/hook")]            // loopback
    [InlineData("https://10.10.10.10/hook")]          // RFC-1918
    [InlineData("https://192.168.0.1/hook")]          // RFC-1918
    [InlineData("https://169.254.169.254/hook")]      // IMDS link-local
    public void Update_SlackWebhookUrlWithSsrfTarget_ThrowsArgumentException(string url)
    {
        var s = NotificationChannelSettings.CreateDefault();

        Assert.Throws<ArgumentException>(() =>
            s.Update(false, null, slackEnabled: true, slackWebhookUrl: url, false, null, false, null));
    }

    [Theory]
    [InlineData("https://hooks.slack.com/services/T/B/X")]
    [InlineData("https://external-monitoring.example.com/webhook/examshield")]
    [InlineData("https://notify.company.org/api/alerts")]
    public void Update_WebhookUrlWithPublicHttpsHost_DoesNotThrow(string url)
    {
        var s = NotificationChannelSettings.CreateDefault();

        var act = () => s.Update(false, null, false, null, false, null, webhookEnabled: true, webhookUrl: url);

        act(); // must not throw
        Assert.Equal(url, s.WebhookUrl);
    }
}
