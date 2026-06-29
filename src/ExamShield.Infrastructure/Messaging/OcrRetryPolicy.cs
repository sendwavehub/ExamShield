namespace ExamShield.Infrastructure.Messaging;

internal static class OcrRetryPolicy
{
    // RabbitMQ serialises AMQP integers as int or long depending on the client version
    internal static int GetRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue("x-retry-count", out var val)) return 0;
        return val switch { int i => i, long l => (int)l, _ => 0 };
    }

    internal static bool ShouldRetry(IDictionary<string, object?>? headers, int maxRetries)
        => GetRetryCount(headers) < maxRetries - 1;
}
