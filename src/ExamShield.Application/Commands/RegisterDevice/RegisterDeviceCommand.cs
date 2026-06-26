using MediatR;

namespace ExamShield.Application.Commands.RegisterDevice;

public sealed record RegisterDeviceCommand(string Name, byte[] PublicKeyBytes) : IRequest<RegisterDeviceResult>;

public sealed record RegisterDeviceResult(Guid DeviceId);
