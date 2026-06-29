using ExamShield.Infrastructure.Http;
using FluentAssertions;
using System.Net;

namespace ExamShield.UnitTests.Infrastructure.Http;

public sealed class ResilienceDelegatingHandlerTests
{
    private static HttpClient BuildClient(Func<HttpRequestMessage, HttpResponseMessage> stub,
        int maxRetries = 3, int cbThreshold = 5, int cbDurationSeconds = 30)
    {
        var handler = new ResilienceDelegatingHandler(maxRetries, cbThreshold, cbDurationSeconds)
        {
            InnerHandler = new StubHandler(stub)
        };
        return new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
    }

    [Fact]
    public async Task SuccessOnFirstAttempt_ReturnsImmediately()
    {
        var calls = 0;
        var client = BuildClient(_ => { calls++; return Ok(); });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        calls.Should().Be(1);
    }

    [Fact]
    public async Task TransientError_RetriesUpToMax_ThenReturnsLastResponse()
    {
        var calls = 0;
        var client = BuildClient(_ =>
        {
            calls++;
            return calls <= 3
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : Ok();
        }, maxRetries: 3);

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        calls.Should().Be(4);  // 3 failures + 1 success
    }

    [Fact]
    public async Task ExceedsMaxRetries_ReturnsLastTransientResponse()
    {
        var calls = 0;
        var client = BuildClient(_ => { calls++; return new HttpResponseMessage(HttpStatusCode.BadGateway); },
            maxRetries: 2);

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        calls.Should().Be(3);  // initial + 2 retries
    }

    [Fact]
    public async Task NetworkException_RetriesAndSucceeds()
    {
        var calls = 0;
        var client = BuildClient(_ =>
        {
            calls++;
            if (calls < 3) throw new HttpRequestException("network error");
            return Ok();
        }, maxRetries: 3);

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        calls.Should().Be(3);
    }

    [Fact]
    public async Task CircuitBreaker_OpensAfterThreshold_ThrowsOnNextCall()
    {
        var client = BuildClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            maxRetries: 0, cbThreshold: 3, cbDurationSeconds: 60);

        // Trip the circuit (3 failures; cbThreshold = 3)
        for (var i = 0; i < 3; i++)
            await client.GetAsync("/");

        // Circuit is now open — next call should be rejected without hitting the inner handler
        var act = () => client.GetAsync("/");
        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("*Circuit*");
    }

    private static HttpResponseMessage Ok() => new(HttpStatusCode.OK);

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> func)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(func(request));
    }
}
