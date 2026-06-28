using ExamShield.Application.Commands.TriggerOcr;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace ExamShield.UnitTests.Application.Commands.TriggerOcr;

public sealed class WatermarkTamperAlertTests
{
    private readonly ICaptureRepository _captures = Substitute.For<ICaptureRepository>();
    private readonly IImageStorage _storage = Substitute.For<IImageStorage>();
    private readonly IWatermarkService _watermark = Substitute.For<IWatermarkService>();
    private readonly IOcrService _ocr = Substitute.For<IOcrService>();
    private readonly IOcrResultRepository _ocrResults = Substitute.For<IOcrResultRepository>();
    private readonly IManualReviewRepository _reviews = Substitute.For<IManualReviewRepository>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly ISystemSettingsRepository _settings = Substitute.For<ISystemSettingsRepository>();
    private readonly ISecurityEventRepository _security = Substitute.For<ISecurityEventRepository>();
    private readonly IAlertService _alerts = Substitute.For<IAlertService>();

    private TriggerOcrCommandHandler MakeSut() =>
        new(_captures, _storage, _watermark, _ocr, _ocrResults, _reviews,
            _auditLog, _settings, _security, _alerts, Substitute.For<IImageEncryptionService>());

    [Fact]
    public async Task Handle_WhenWatermarkInvalid_SendsTamperingAlert()
    {
        var capture = Capture.Create(ExamId.New(), StudentId.New(), DeviceId.New(),
            new PageNumber(1), Hash.FromHex("a".PadRight(64, 'a')), new Signature(new byte[64]));
        capture.RecordUpload("key/1");

        _captures.GetByIdAsync(Arg.Any<CaptureId>(), Arg.Any<CancellationToken>()).Returns(capture);
        _storage.RetrieveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([0x01, 0x02]);
        _watermark.Extract(Arg.Any<byte[]>()).Returns(WatermarkExtractionResult.Failure());
        _settings.GetAsync(Arg.Any<CancellationToken>()).Returns(SystemSettings.CreateDefault());

        await Assert.ThrowsAsync<WatermarkTamperedException>(() =>
            MakeSut().Handle(new TriggerOcrCommand(capture.Id.Value), default));

        await _alerts.Received(1).SendAsync(
            AlertType.TamperingDetected,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWatermarkValid_DoesNotSendTamperingAlert()
    {
        var capture = Capture.Create(ExamId.New(), StudentId.New(), DeviceId.New(),
            new PageNumber(1), Hash.FromHex("a".PadRight(64, 'a')), new Signature(new byte[64]));
        capture.RecordUpload("key/1");

        var payload = new WatermarkPayload
        {
            ExamId = capture.ExamId.Value,
            CaptureId = capture.Id.Value,
            DeviceId = capture.DeviceId.Value,
            TimestampUtcTicks = DateTimeOffset.UtcNow.UtcTicks,
            Nonce = "n",
            ImageHash = "h"
        };
        _captures.GetByIdAsync(Arg.Any<CaptureId>(), Arg.Any<CancellationToken>()).Returns(capture);
        _storage.RetrieveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([0x01, 0x02]);
        _watermark.Extract(Arg.Any<byte[]>()).Returns(WatermarkExtractionResult.Success(payload, 2));
        _settings.GetAsync(Arg.Any<CancellationToken>()).Returns(SystemSettings.CreateDefault());
        _ocr.ExtractAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new OcrExtractionResult([]));

        await MakeSut().Handle(new TriggerOcrCommand(capture.Id.Value), default);

        await _alerts.DidNotReceive().SendAsync(
            AlertType.TamperingDetected, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
