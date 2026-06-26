using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.RegisterCapture;

public sealed class RegisterCaptureCommandHandler
    : IRequestHandler<RegisterCaptureCommand, RegisterCaptureResult>
{
    private readonly ICaptureRepository _repository;
    private readonly IDeviceRepository _devices;
    private readonly ISignatureVerificationService _sigService;
    private readonly IAuditLogRepository _auditLog;

    public RegisterCaptureCommandHandler(
        ICaptureRepository repository,
        IDeviceRepository devices,
        ISignatureVerificationService sigService,
        IAuditLogRepository auditLog)
    {
        _repository = repository;
        _devices = devices;
        _sigService = sigService;
        _auditLog = auditLog;
    }

    public async Task<RegisterCaptureResult> Handle(
        RegisterCaptureCommand command, CancellationToken ct)
    {
        // Validate value objects first — fast fail before any I/O
        var hash = Hash.FromHex(command.HashHex);
        var signature = new Signature(command.SignatureBytes);
        var examId = new ExamId(command.ExamId);
        var studentId = new StudentId(command.StudentId);
        var deviceId = new DeviceId(command.DeviceId);
        var pageNumber = new PageNumber(command.PageNumber);

        var device = await _devices.GetByIdAsync(deviceId, ct)
            ?? throw new DeviceNotFoundException(command.DeviceId);

        if (!_sigService.Verify(hash, signature, device.PublicKey))
            throw new InvalidSignatureException(command.DeviceId);

        var capture = Capture.Create(examId, studentId, deviceId, pageNumber, hash, signature);

        await _repository.AddAsync(capture, ct);
        await _auditLog.AppendAsync(
            AuditLog.Record(AuditAction.CaptureRegistered, captureId: capture.Id), ct);

        return new RegisterCaptureResult(capture.Id.Value);
    }
}
