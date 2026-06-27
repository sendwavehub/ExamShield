using System.Net;
using ExamShield.Domain.Enums;
using ExamShield.Infrastructure.Alerts;
using FluentAssertions;
using Xunit;

namespace ExamShield.UnitTests.Infrastructure.Alerts;

public sealed class SlackAlertChannelTests
{
    private readonly List<(HttpMethod Method, Uri Uri, string Body)> _captured = [];
    private readonly SlackAlertChannel _sut;

    public SlackAlertChannelTests()
    {
        var handler = new CapturingHandler(_captured);
        _sut = new SlackAlertChannel(new HttpClient(handler), "https://hooks.slack.com/test-webhook");
    }

    [Fact]
    public async Task SendAsync_PostsToWebhookUrl()
    {
        await _sut.SendAsync(AlertType.HashMismatch, "Hash mismatch detected.");

        _captured.Should().HaveCount(1);
        _captured[0].Uri.ToString().Should().Be("https://hooks.slack.com/test-webhook");
        _captured[0].Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task SendAsync_BodyContainsAlertType()
    {
        await _sut.SendAsync(AlertType.TamperingDetected, "Image tampered.");

        _captured[0].Body.Should().Contain("TamperingDetected");
    }

    [Fact]
    public async Task SendAsync_BodyContainsMessage()
    {
        await _sut.SendAsync(AlertType.DuplicateUpload, "Duplicate upload detected.");

        _captured[0].Body.Should().Contain("Duplicate upload detected.");
    }

    [Fact]
    public async Task SendAsync_WhenHttpFails_DoesNotThrow()
    {
        var channel = new SlackAlertChannel(
            new HttpClient(new FailingHandler()), "https://hooks.slack.com/test");

        await ((Func<Task>)(() => channel.SendAsync(AlertType.HashMismatch, "msg")))
            .Should().NotThrowAsync();
    }

    private sealed class CapturingHandler(List<(HttpMethod, Uri, string)> captured)
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is not null
                ? await request.Content.ReadAsStringAsync(ct) : "";
            captured.Add((request.Method, request.RequestUri!, body));
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct) =>
            throw new HttpRequestException("Connection refused.");
    }
}
