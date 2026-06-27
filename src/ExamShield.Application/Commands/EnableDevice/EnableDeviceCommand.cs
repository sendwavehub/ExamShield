using MediatR;

namespace ExamShield.Application.Commands.EnableDevice;

public sealed record EnableDeviceCommand(Guid DeviceId) : IRequest;
