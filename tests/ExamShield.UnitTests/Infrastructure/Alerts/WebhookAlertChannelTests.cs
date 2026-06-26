using System.Net;
using System.Text.Json;
using ExamShield.Domain.Enums;
using ExamShield.Infrastructure.Alerts;
using FluentAssertions;

namespace ExamShield.UnitTests.Infrastructure.Alerts;

public sealed class WebhookAlertChannelTests
{
    private readonly List<(HttpMethod Method, Uri Uri, string Body)> _captured = [];
    private readonly WebhookAlertChannel _sut;

    public WebhookAlertChannelTests()
    {
        var handler = new CapturingHandler(_captured);
        var client = new HttpClient(handler);
        _sut = new WebhookAlertChannel(client, "https://hooks.example.com/examshield");
    }

    [Fact]
    public async Task SendAsync_MakesPostRequestToConfiguredUrl()
    {
        await _sut.SendAsync(AlertType.HashMismatch, "Hash mismatch detected.");

        _captured.Should().HaveCount(1);
        _captured[0].Method.Should().Be(HttpMethod.Post);
        _captured[0].Uri.ToString().Should().Be("https://hooks.example.com/examshield");
    }

    [Fact]
    public async Task SendAsync_BodyContainsAlertTypeAndMessage()
    {
        await _sut.SendAsync(AlertType.TamperingDetected, "Image tampered.");

        var body = JsonDocument.Parse(_captured[0].Body).RootElement;
        body.GetProperty("alertType").GetString().Should().Be("TamperingDetected");
        body.GetProperty("message").GetString().Should().Be("Image tampered.");
    }

    [Fact]
    public async Task SendAsync_WhenHttpFails_DoesNotThrow()
    {
        var handler = new FailingHandler();
        var client = new HttpClient(handler);
        var channel = new WebhookAlertChannel(client, "https://hooks.example.com/bad");

        var act = () => channel.SendAsync(AlertType.HashMismatch, "msg");

        await act.Should().NotThrowAsync();
    }

    private sealed class CapturingHandler(List<(HttpMethod, Uri, string)> captured)
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is not null
                ? await request.Content.ReadAsStringAsync(ct)
                : "";
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
