using System.Text.Json;
using ExamShield.Application.Commands.TriggerOcr;
using ExamShield.Application.Messages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ExamShield.Infrastructure.Messaging;

public sealed class OcrConsumerService(
    IConnectionFactory factory,
    MessagingOptions options,
    IServiceProvider services,
    ILogger<OcrConsumerService> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel?    _channel;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _connection = await factory.CreateConnectionAsync(ct);
        _channel    = await _connection.CreateChannelAsync(cancellationToken: ct);

        await OcrQueueSetup.DeclareAsync(_channel, options, ct);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: options.OcrQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);

        await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var retryCount = OcrRetryPolicy.GetRetryCount(ea.BasicProperties.Headers);

        try
        {
            var msg = JsonSerializer.Deserialize<OcrRequestedMessage>(ea.Body.Span);
            if (msg is not null)
            {
                using var scope = services.CreateScope();
                var mediator    = scope.ServiceProvider.GetRequiredService<ISender>();
                await mediator.Send(new TriggerOcrCommand(msg.CaptureId));
            }

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OCR processing failed (attempt {Attempt}/{Max})",
                retryCount + 1, options.MaxOcrRetries);

            if (OcrRetryPolicy.ShouldRetry(ea.BasicProperties.Headers, options.MaxOcrRetries))
            {
                await RepublishForRetryAsync(ea, retryCount + 1);
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            else
            {
                logger.LogCritical(
                    "OCR message exhausted {Max} attempts — routing to DLQ: delivery tag {Tag}",
                    options.MaxOcrRetries, ea.DeliveryTag);
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }
    }

    private async Task RepublishForRetryAsync(BasicDeliverEventArgs ea, int newRetryCount)
    {
        IDictionary<string, object?> source = ea.BasicProperties.Headers
            ?? new Dictionary<string, object?>();
        var headers = new Dictionary<string, object?>(source) { ["x-retry-count"] = newRetryCount };
        var props = new BasicProperties { Persistent = true, Headers = headers };
        await _channel!.BasicPublishAsync(
            exchange: "",
            routingKey: options.OcrQueueName,
            mandatory: false,
            basicProperties: props,
            body: ea.Body,
            cancellationToken: default);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
