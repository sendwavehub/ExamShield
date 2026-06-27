using System.Net;
using ExamShield.Domain.Enums;
using ExamShield.Infrastructure.Alerts;
using FluentAssertions;
using Xunit;

namespace ExamShield.UnitTests.Infrastructure.Alerts;

public sealed class LineNotifyAlertChannelTests
{
    private readonly List<(HttpMethod Method, Uri Uri, string Body, string Auth)> _captured = [];
    private readonly LineNotifyAlertChannel _sut;

    public LineNotifyAlertChannelTests()
    {
        var handler = new CapturingHandler(_captured);
        _sut = new LineNotifyAlertChannel(new HttpClient(handler), "test-token-abc");
    }

    [Fact]
    public async Task SendAsync_PostsToLineNotifyEndpoint()
    {
        await _sut.SendAsync(AlertType.HashMismatch, "Hash mismatch detected.");

        _captured.Should().HaveCount(1);
        _captured[0].Uri.ToString().Should().Be("https://notify-api.line.me/api/notify");
        _captured[0].Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task SendAsync_UsesBearerTokenAuth()
    {
        await _sut.SendAsync(AlertType.HashMismatch, "msg");

        _captured[0].Auth.Should().Be("Bearer test-token-abc");
    }

    [Fact]
    public async Task SendAsync_BodyContainsMessage()
    {
        await _sut.SendAsync(AlertType.TamperingDetected, "Image tampered.");

        // FormUrlEncodedContent encodes the body — decode before asserting.
        var decoded = Uri.UnescapeDataString(_captured[0].Body.Replace("+", " "));
        decoded.Should().Contain("Image tampered.");
    }

    [Fact]
    public async Task SendAsync_WhenHttpFails_DoesNotThrow()
    {
        var channel = new LineNotifyAlertChannel(
            new HttpClient(new FailingHandler()), "token");

        await ((Func<Task>)(() => channel.SendAsync(AlertType.HashMismatch, "msg")))
            .Should().NotThrowAsync();
    }

    private sealed class CapturingHandler(List<(HttpMethod, Uri, string, string)> captured)
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is not null
                ? await request.Content.ReadAsStringAsync(ct) : "";
            var auth = request.Headers.Authorization?.ToString() ?? "";
            captured.Add((request.Method, request.RequestUri!, body, auth));
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
