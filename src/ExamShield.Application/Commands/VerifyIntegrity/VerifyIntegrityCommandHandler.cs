using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.Services;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.VerifyIntegrity;

public sealed class VerifyIntegrityCommandHandler
    : IRequestHandler<VerifyIntegrityCommand, VerifyIntegrityResult>
{
    private readonly ICaptureRepository _repository;
    private readonly HashVerificationService _hashService;
    private readonly IAuditLogRepository _auditLog;
    private readonly IAlertService _alertService;

    public VerifyIntegrityCommandHandler(
        ICaptureRepository repository,
        HashVerificationService hashService,
        IAuditLogRepository auditLog,
        IAlertService alertService)
    {
        _repository = repository;
        _hashService = hashService;
        _auditLog = auditLog;
        _alertService = alertService;
    }

    public async Task<VerifyIntegrityResult> Handle(
        VerifyIntegrityCommand command, CancellationToken ct)
    {
        var actualHash = _hashService.ComputeHash(command.ImageBytes);

        var capture = await _repository.GetByIdAsync(new CaptureId(command.CaptureId), ct)
            ?? throw new CaptureNotFoundException(command.CaptureId);

        // Compare only — never mutate capture status based on client-supplied bytes.
        // Only server-side re-verification (GET /verify/{id}) or watermark checks may set Tampered.
        var isValid = actualHash == capture.ExpectedHash;

        if (!isValid)
            await _alertService.SendAsync(AlertType.TamperingDetected,
                $"Client-side hash mismatch reported for capture {capture.Id.Value}.", ct);

        await _auditLog.AppendAsync(
            AuditLog.Record(
                isValid ? AuditAction.HashVerified : AuditAction.TamperingDetected,
                captureId: capture.Id), ct);

        return new VerifyIntegrityResult(isValid, capture.ExpectedHash.Hex, actualHash.Hex);
    }
}
