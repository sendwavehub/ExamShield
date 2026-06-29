using RabbitMQ.Client;

namespace ExamShield.Infrastructure.Messaging;

internal static class OcrQueueSetup
{
    internal static async Task DeclareAsync(IChannel channel, MessagingOptions options, CancellationToken ct)
    {
        // Dead-letter exchange — receives messages that exhaust all retries
        await channel.ExchangeDeclareAsync(
            exchange: options.OcrDlxName,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: ct);

        // DLQ bound to the DLX; permanently-failed messages land here for manual inspection
        await channel.QueueDeclareAsync(
            queue: options.OcrDlqName,
            durable: true, exclusive: false, autoDelete: false,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: options.OcrDlqName,
            exchange: options.OcrDlxName,
            routingKey: options.OcrQueueName,
            cancellationToken: ct);

        // Main queue with DLX pointer — nack(requeue:false) routes to DLX → DLQ
        await channel.QueueDeclareAsync(
            queue: options.OcrQueueName,
            durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"]     = options.OcrDlxName,
                ["x-dead-letter-routing-key"]  = options.OcrQueueName,
            },
            cancellationToken: ct);
    }
}
