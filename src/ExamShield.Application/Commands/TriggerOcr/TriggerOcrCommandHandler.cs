using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.TriggerOcr;

public sealed class TriggerOcrCommandHandler : IRequestHandler<TriggerOcrCommand, TriggerOcrResult>
{
    private readonly ICaptureRepository _captures;
    private readonly IImageStorage _imageStorage;
    private readonly IWatermarkService _watermark;
    private readonly IOcrService _ocrService;
    private readonly IOcrResultRepository _ocrResults;
    private readonly IManualReviewRepository _manualReviews;
    private readonly IAuditLogRepository _auditLog;
    private readonly ISystemSettingsRepository _systemSettings;
    private readonly ISecurityEventRepository _securityEvents;
    private readonly IAlertService _alerts;

    public TriggerOcrCommandHandler(
        ICaptureRepository captures, IImageStorage imageStorage,
        IWatermarkService watermark, IOcrService ocrService,
        IOcrResultRepository ocrResults, IManualReviewRepository manualReviews,
        IAuditLogRepository auditLog, ISystemSettingsRepository systemSettings,
        ISecurityEventRepository securityEvents, IAlertService alerts)
    {
        _captures = captures;
        _imageStorage = imageStorage;
        _watermark = watermark;
        _ocrService = ocrService;
        _ocrResults = ocrResults;
        _manualReviews = manualReviews;
        _auditLog = auditLog;
        _systemSettings = systemSettings;
        _securityEvents = securityEvents;
        _alerts = alerts;
    }

    public async Task<TriggerOcrResult> Handle(TriggerOcrCommand command, CancellationToken ct)
    {
        var capture = await _captures.GetByIdAsync(new CaptureId(command.CaptureId), ct)
            ?? throw new CaptureNotFoundException(command.CaptureId);

        if (capture.Status == CaptureStatus.Tampered)
            throw new CaptureAlreadyTamperedException(command.CaptureId);

        if (capture.StorageKey is null)
            throw new CaptureNotUploadedException(command.CaptureId);

        var existingResult = await _ocrResults.GetByCaptureIdAsync(capture.Id, ct);
        if (existingResult is not null)
            throw new DuplicateOcrException(command.CaptureId);

        var settings    = await _systemSettings.GetAsync(ct);
        var storedBytes = await _imageStorage.RetrieveAsync(capture.StorageKey, ct);
        var wm          = _watermark.Extract(storedBytes);

        if (!wm.IsValid)
        {
            capture.FlagAsTampered("Watermark validation failed during OCR");
            await _captures.UpdateAsync(capture, ct);
            await _securityEvents.AddAsync(
                SecurityEvent.Create(SecurityEventType.WatermarkTampered, SecuritySeverity.Critical,
                    $"Watermark invalid for capture {capture.Id.Value}",
                    captureId: capture.Id.Value), ct);
            await _auditLog.AppendAsync(
                AuditLog.Record(AuditAction.TamperingDetected, captureId: capture.Id), ct);
            await _alerts.SendAsync(AlertType.TamperingDetected,
                $"Watermark tampered on capture {capture.Id.Value}", ct);
            throw new WatermarkTamperedException(capture.Id.Value);
        }

        var imageBytes = storedBytes[..wm.OriginalImageLength];
        var extraction = await _ocrService.ExtractAsync(imageBytes, ct);
        var ocrResult  = OcrResult.Create(capture.Id, extraction.Answers, settings.OcrConfidenceThreshold);

        await _ocrResults.AddAsync(ocrResult, ct);

        if (ocrResult.RequiresManualReview)
        {
            var review = ManualReview.CreateFor(ocrResult);
            await _manualReviews.AddAsync(review, ct);
            await _auditLog.AppendAsync(
                AuditLog.Record(AuditAction.ManualReviewStarted, captureId: capture.Id), ct);
        }

        await _auditLog.AppendAsync(
            AuditLog.Record(AuditAction.OCRCompleted, captureId: capture.Id), ct);

        return new TriggerOcrResult(ocrResult.Id.Value, ocrResult.Status, ocrResult.RequiresManualReview);
    }
}
