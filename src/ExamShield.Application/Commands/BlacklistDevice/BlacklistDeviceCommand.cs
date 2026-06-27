using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.BlacklistDevice;

public sealed record BlacklistDeviceCommand(Guid DeviceId, string Reason) : IRequest;

public sealed class BlacklistDeviceCommandHandler(IDeviceRepository devices)
    : IRequestHandler<BlacklistDeviceCommand>
{
    public async Task Handle(BlacklistDeviceCommand command, CancellationToken ct)
    {
        var device = await devices.GetByIdAsync(new DeviceId(command.DeviceId), ct)
            ?? throw new KeyNotFoundException($"Device '{command.DeviceId}' not found.");

        device.Blacklist(command.Reason);
        await devices.UpdateAsync(device, ct);
    }
}
