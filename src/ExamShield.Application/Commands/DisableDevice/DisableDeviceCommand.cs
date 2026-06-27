using MediatR;

namespace ExamShield.Application.Commands.DisableDevice;

public sealed record DisableDeviceCommand(Guid DeviceId) : IRequest;
