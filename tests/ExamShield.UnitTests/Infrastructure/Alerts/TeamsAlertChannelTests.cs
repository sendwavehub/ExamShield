using System.Net;
using System.Text.Json;
using ExamShield.Domain.Enums;
using ExamShield.Infrastructure.Alerts;
using FluentAssertions;
using Xunit;

namespace ExamShield.UnitTests.Infrastructure.Alerts;

public sealed class TeamsAlertChannelTests
{
    private readonly List<(HttpMethod Method, Uri Uri, string Body)> _captured = [];
    private readonly TeamsAlertChannel _sut;

    public TeamsAlertChannelTests()
    {
        var handler = new CapturingHandler(_captured);
        _sut = new TeamsAlertChannel(new HttpClient(handler), "https://teams.example.com/webhook");
    }

    [Fact]
    public async Task SendAsync_PostsToConfiguredUrl()
    {
        await _sut.SendAsync(AlertType.HashMismatch, "msg");

        _captured[0].Uri.ToString().Should().Be("https://teams.example.com/webhook");
        _captured[0].Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task SendAsync_BodyIsMessageCardWithSummary()
    {
        await _sut.SendAsync(AlertType.TamperingDetected, "Image tampered.");

        var doc = JsonDocument.Parse(_captured[0].Body).RootElement;
        doc.GetProperty("@type").GetString().Should().Be("MessageCard");
        doc.GetProperty("summary").GetString().Should().Contain("TamperingDetected");
    }

    [Fact]
    public async Task SendAsync_WhenHttpFails_DoesNotThrow()
    {
        var channel = new TeamsAlertChannel(
            new HttpClient(new FailingHandler()), "https://teams.example.com/webhook");

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
