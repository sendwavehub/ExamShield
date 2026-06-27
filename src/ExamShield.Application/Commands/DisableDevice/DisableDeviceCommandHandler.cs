using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.DisableDevice;

public sealed class DisableDeviceCommandHandler(IDeviceRepository devices) : IRequestHandler<DisableDeviceCommand>
{
    public async Task Handle(DisableDeviceCommand request, CancellationToken ct)
    {
        var device = await devices.GetByIdAsync(new DeviceId(request.DeviceId), ct)
            ?? throw new DeviceNotFoundException(request.DeviceId);
        device.Disable();
        await devices.SaveAsync(device, ct);
    }
}
