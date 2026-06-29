namespace ExamShield.Infrastructure.Http;

/// <summary>
/// Adds exponential-backoff retry + simple circuit-breaker to any named HttpClient.
/// Retries on transient HTTP errors (5xx, 408, 429) and network failures.
/// After <paramref name="circuitBreakerThreshold"/> consecutive failures the circuit
/// opens for <paramref name="circuitBreakerDuration"/> before allowing a probe.
/// </summary>
internal sealed class ResilienceDelegatingHandler(
    int maxRetries = 3,
    int circuitBreakerThreshold = 5,
    int circuitBreakerDurationSeconds = 30) : DelegatingHandler
{
    private int _consecutiveFailures;
    private DateTime _circuitOpenUntil = DateTime.MinValue;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        if (DateTime.UtcNow < _circuitOpenUntil)
            throw new HttpRequestException("Circuit is open — too many consecutive failures.");

        var attempt = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var response = await base.SendAsync(request, ct);
                if (IsTransient(response) && attempt < maxRetries)
                {
                    await DelayAsync(attempt, ct);
                    attempt++;
                    continue;
                }

                if (response.IsSuccessStatusCode)
                    Interlocked.Exchange(ref _consecutiveFailures, 0);
                else
                    RecordFailure();

                return response;
            }
            catch (HttpRequestException) when (attempt < maxRetries)
            {
                RecordFailure();
                await DelayAsync(attempt, ct);
                attempt++;
            }
        }
    }

    private void RecordFailure()
    {
        if (Interlocked.Increment(ref _consecutiveFailures) >= circuitBreakerThreshold)
            _circuitOpenUntil = DateTime.UtcNow.AddSeconds(circuitBreakerDurationSeconds);
    }

    private static bool IsTransient(HttpResponseMessage r)
        => r.StatusCode is System.Net.HttpStatusCode.InternalServerError
            or System.Net.HttpStatusCode.BadGateway
            or System.Net.HttpStatusCode.ServiceUnavailable
            or System.Net.HttpStatusCode.GatewayTimeout
            or System.Net.HttpStatusCode.RequestTimeout
            or System.Net.HttpStatusCode.TooManyRequests;

    private static Task DelayAsync(int attempt, CancellationToken ct)
        => Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)), ct);
}
