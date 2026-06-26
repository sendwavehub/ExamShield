using System.Security.Cryptography;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.Services;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.UploadImage;

public sealed class UploadImageCommandHandler : IRequestHandler<UploadImageCommand, UploadImageResult>
{
    private readonly ICaptureRepository _repository;
    private readonly HashVerificationService _hashService;
    private readonly IImageStorage _imageStorage;
    private readonly IAuditLogRepository _auditLog;
    private readonly IWatermarkService _watermarkService;

    public UploadImageCommandHandler(
        ICaptureRepository repository,
        HashVerificationService hashService,
        IImageStorage imageStorage,
        IAuditLogRepository auditLog,
        IWatermarkService watermarkService)
    {
        _repository = repository;
        _hashService = hashService;
        _imageStorage = imageStorage;
        _auditLog = auditLog;
        _watermarkService = watermarkService;
    }

    public async Task<UploadImageResult> Handle(UploadImageCommand command, CancellationToken ct)
    {
        if (command.ImageBytes.Length == 0)
            throw new ArgumentException("Image bytes cannot be empty.", nameof(command.ImageBytes));

        var capture = await _repository.GetByIdAsync(new CaptureId(command.CaptureId), ct)
            ?? throw new CaptureNotFoundException(command.CaptureId);

        var actualHash = _hashService.ComputeHash(command.ImageBytes);
        if (actualHash != capture.ExpectedHash)
            throw new HashMismatchException(command.CaptureId, capture.ExpectedHash, actualHash);

        var payload = new WatermarkPayload
        {
            ExamId = capture.ExamId.Value,
            CaptureId = capture.Id.Value,
            TimestampUtcTicks = DateTimeOffset.UtcNow.UtcTicks,
            Nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
            ImageHash = capture.ExpectedHash.Hex
        };
        var watermarkedBytes = _watermarkService.Embed(command.ImageBytes, payload);

        var storageKey = await _imageStorage.StoreAsync(command.CaptureId, watermarkedBytes, ct);

        capture.RecordUpload(storageKey);

        await _repository.UpdateAsync(capture, ct);
        await _auditLog.AppendAsync(
            AuditLog.Record(AuditAction.ImageUploaded, captureId: capture.Id), ct);

        return new UploadImageResult(storageKey);
    }
}
