using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.Services;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Queries.ServerVerifyCapture;

public sealed class ServerVerifyCaptureQueryHandler
    : IRequestHandler<ServerVerifyCaptureQuery, ServerVerifyResult>
{
    private readonly ICaptureRepository _captures;
    private readonly IImageStorage _imageStorage;
    private readonly HashVerificationService _hashService;
    private readonly IDeviceRepository _devices;
    private readonly ISignatureVerificationService _sigService;
    private readonly IAuditLogRepository _auditLog;
    private readonly IAlertService _alertService;
    private readonly IWatermarkService _watermarkService;

    public ServerVerifyCaptureQueryHandler(
        ICaptureRepository captures, IImageStorage imageStorage,
        HashVerificationService hashService, IDeviceRepository devices,
        ISignatureVerificationService sigService, IAuditLogRepository auditLog,
        IAlertService alertService, IWatermarkService watermarkService)
    {
        _captures = captures;
        _imageStorage = imageStorage;
        _hashService = hashService;
        _devices = devices;
        _sigService = sigService;
        _auditLog = auditLog;
        _alertService = alertService;
        _watermarkService = watermarkService;
    }

    public async Task<ServerVerifyResult> Handle(
        ServerVerifyCaptureQuery query, CancellationToken ct)
    {
        var capture = await _captures.GetByIdAsync(new CaptureId(query.CaptureId), ct)
            ?? throw new CaptureNotFoundException(query.CaptureId);

        if (capture.StorageKey is null)
            throw new CaptureNotUploadedException(query.CaptureId);

        var storedBytes = await _imageStorage.RetrieveAsync(capture.StorageKey, ct);

        bool hashValid;
        var extraction = _watermarkService.Extract(storedBytes);
        if (!extraction.IsValid)
        {
            hashValid = false;
        }
        else
        {
            var originalBytes = storedBytes[..extraction.OriginalImageLength];
            var actualHash = _hashService.ComputeHash(originalBytes);
            hashValid = actualHash == capture.ExpectedHash;
        }

        var device = await _devices.GetByIdAsync(capture.DeviceId, ct);
        var signatureValid = device is not null &&
            _sigService.Verify(capture.ExpectedHash, capture.Signature, device.PublicKey);

        var isValid = hashValid && signatureValid;

        if (!isValid)
            await _alertService.SendAsync(AlertType.TamperingDetected,
                $"Server re-verify detected tampering on capture {capture.Id.Value}.", ct);

        var auditAction = isValid ? AuditAction.HashVerified : AuditAction.TamperingDetected;
        await _auditLog.AppendAsync(
            AuditLog.Record(auditAction, captureId: capture.Id), ct);

        var displayHash = extraction.IsValid
            ? _hashService.ComputeHash(storedBytes[..extraction.OriginalImageLength]).Hex
            : string.Empty;

        return new ServerVerifyResult(
            isValid, hashValid, signatureValid,
            capture.StorageKey, capture.ExpectedHash.Hex, displayHash);
    }
}
