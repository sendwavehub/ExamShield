using ExamShield.Application.Commands.TriggerOcr;
using ExamShield.Application.Messages;
using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Infrastructure.Messaging;

public sealed class InMemoryMessagePublisher(ISender sender) : IMessagePublisher
{
    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class =>
        message switch
        {
            OcrRequestedMessage msg => sender.Send(new TriggerOcrCommand(msg.CaptureId), ct),
            _ => Task.CompletedTask
        };
}
