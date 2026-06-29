namespace ExamShield.Infrastructure.Messaging;

public sealed class MessagingOptions
{
    public const string Section = "Messaging";
    public string Type { get; init; } = "InMemory";
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";
    public string OcrQueueName { get; init; } = "ocr.requests";
    public string OcrDlxName { get; init; } = "ocr.dlx";
    public string OcrDlqName { get; init; } = "ocr.dlq";

    // Deliver → DLQ after this many attempts (1 = no retry, 3 = two retries then DLQ)
    public int MaxOcrRetries { get; init; } = 3;
}
