using System.Net;
using System.Text.Json;
using ExamShield.Infrastructure.OCR;
using FluentAssertions;
using Xunit;

namespace ExamShield.UnitTests.Infrastructure.OCR;

public sealed class HttpOcrServiceTests
{
    private static readonly byte[] ImageBytes = "fake-image"u8.ToArray();

    private static HttpClient ClientReturning(string json)
    {
        var handler = new StaticResponseHandler(json);
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
    }

    [Fact]
    public async Task ExtractAsync_PostsRawBytesToConfiguredEndpoint()
    {
        var captured = new List<(HttpMethod Method, Uri Uri, byte[] Body)>();
        var client = new HttpClient(new CapturingHandler(captured))
        {
            BaseAddress = new Uri("http://ocr-svc")
        };
        var sut = new HttpOcrService(client, new OcrOptions { Endpoint = "/extract" });

        await sut.ExtractAsync(ImageBytes);

        captured.Should().HaveCount(1);
        captured[0].Method.Should().Be(HttpMethod.Post);
        captured[0].Uri.PathAndQuery.Should().Be("/extract");
        captured[0].Body.Should().BeEquivalentTo(ImageBytes);
    }

    [Fact]
    public async Task ExtractAsync_MapsResponseToExtractedAnswers()
    {
        var json = """
            {
              "answers": [
                { "questionNumber": 1, "selectedOption": "A", "confidence": 0.95 },
                { "questionNumber": 2, "selectedOption": "B", "confidence": 0.80 }
              ]
            }
            """;
        var sut = new HttpOcrService(ClientReturning(json), new OcrOptions { Endpoint = "/extract" });

        var result = await sut.ExtractAsync(ImageBytes);

        result.Answers.Should().HaveCount(2);
        result.Answers[0].QuestionNumber.Should().Be(1);
        result.Answers[0].Text.Should().Be("A");
        result.Answers[0].Confidence.Value.Should().Be(0.95);
    }

    [Fact]
    public async Task ExtractAsync_WhenServerReturnsError_ThrowsHttpRequestException()
    {
        var client = new HttpClient(new ErrorHandler()) { BaseAddress = new Uri("http://ocr-svc") };
        var sut = new HttpOcrService(client, new OcrOptions { Endpoint = "/extract" });

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.ExtractAsync(ImageBytes));
    }

    private sealed class StaticResponseHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage req, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
    }

    private sealed class CapturingHandler(List<(HttpMethod, Uri, byte[])> captured)
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage req, CancellationToken ct)
        {
            var body = req.Content is not null ? await req.Content.ReadAsByteArrayAsync(ct) : [];
            captured.Add((req.Method, req.RequestUri!, body));
            var json = """{"answers":[{"questionNumber":1,"selectedOption":"A","confidence":0.9}]}""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class ErrorHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage req, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
    }
}
