using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.RegisterDevice;

public sealed class RegisterDeviceCommandHandler : IRequestHandler<RegisterDeviceCommand, RegisterDeviceResult>
{
    private readonly IDeviceRepository _repository;
    private readonly IAuditLogRepository _auditLog;

    public RegisterDeviceCommandHandler(IDeviceRepository repository, IAuditLogRepository auditLog)
    {
        _repository = repository;
        _auditLog = auditLog;
    }

    public async Task<RegisterDeviceResult> Handle(RegisterDeviceCommand command, CancellationToken ct)
    {
        var device = Device.Register(command.Name, new PublicKey(command.PublicKeyBytes));

        await _repository.AddAsync(device, ct);
        await _auditLog.AppendAsync(
            AuditLog.Record(AuditAction.DeviceRegistered), ct);

        return new RegisterDeviceResult(device.Id.Value);
    }
}
