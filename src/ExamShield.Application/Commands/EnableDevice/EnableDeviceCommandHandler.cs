using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.EnableDevice;

public sealed class EnableDeviceCommandHandler(IDeviceRepository devices) : IRequestHandler<EnableDeviceCommand>
{
    public async Task Handle(EnableDeviceCommand request, CancellationToken ct)
    {
        var device = await devices.GetByIdAsync(new DeviceId(request.DeviceId), ct)
            ?? throw new DeviceNotFoundException(request.DeviceId);
        device.Enable();
        await devices.SaveAsync(device, ct);
    }
}
