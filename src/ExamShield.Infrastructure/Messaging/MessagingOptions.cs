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
}
