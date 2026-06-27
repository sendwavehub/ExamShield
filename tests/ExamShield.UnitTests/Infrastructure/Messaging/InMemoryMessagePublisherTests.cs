using ExamShield.Application.Messages;
using ExamShield.Infrastructure.Messaging;
using MediatR;
using NSubstitute;
using Xunit;
using ExamShield.Application.Commands.TriggerOcr;

namespace ExamShield.UnitTests.Infrastructure.Messaging;

public sealed class InMemoryMessagePublisherTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly InMemoryMessagePublisher _sut;

    public InMemoryMessagePublisherTests() => _sut = new InMemoryMessagePublisher(_sender);

    [Fact]
    public async Task PublishAsync_OcrRequestedMessage_DispatchesTriggerOcrCommand()
    {
        var captureId = Guid.NewGuid();

        await _sut.PublishAsync(new OcrRequestedMessage(captureId));

        await _sender.Received(1).Send(
            Arg.Is<TriggerOcrCommand>(c => c.CaptureId == captureId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_UnknownMessage_DoesNotDispatch()
    {
        await _sut.PublishAsync(new { Unknown = true });

        await _sender.DidNotReceive().Send(Arg.Any<IBaseRequest>(), Arg.Any<CancellationToken>());
    }
}
