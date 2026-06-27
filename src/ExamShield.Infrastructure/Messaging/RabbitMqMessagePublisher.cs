using System.Text.Json;
using ExamShield.Domain.Interfaces;
using RabbitMQ.Client;

namespace ExamShield.Infrastructure.Messaging;

public sealed class RabbitMqMessagePublisher(IConnectionFactory factory, MessagingOptions options)
    : IMessagePublisher
{
    public async Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        await using var connection = await factory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: options.OcrQueueName,
            durable: true, exclusive: false, autoDelete: false,
            cancellationToken: ct);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var props = new BasicProperties { Persistent = true };

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: options.OcrQueueName,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }
}
