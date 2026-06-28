using ExamShield.Application.Commands.TriggerOcr;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands;

public sealed class TriggerOcrTamperedCaptureTests
{
    private readonly ICaptureRepository      _captures   = Substitute.For<ICaptureRepository>();
    private readonly IImageStorage           _storage    = Substitute.For<IImageStorage>();
    private readonly IWatermarkService       _watermark  = Substitute.For<IWatermarkService>();
    private readonly IOcrService             _ocr        = Substitute.For<IOcrService>();
    private readonly IOcrResultRepository    _ocrResults = Substitute.For<IOcrResultRepository>();
    private readonly IManualReviewRepository _reviews    = Substitute.For<IManualReviewRepository>();
    private readonly IAuditLogRepository     _auditLog   = Substitute.For<IAuditLogRepository>();
    private readonly ISystemSettingsRepository _settings = Substitute.For<ISystemSettingsRepository>();
    private readonly ISecurityEventRepository _secEvents = Substitute.For<ISecurityEventRepository>();
    private readonly IAlertService _alerts = Substitute.For<IAlertService>();
    private readonly IImageEncryptionService _encryption = Substitute.For<IImageEncryptionService>();
    private readonly TriggerOcrCommandHandler _sut;

    public TriggerOcrTamperedCaptureTests()
    {
        _settings.GetAsync(default).Returns(SystemSettings.CreateDefault());
        _sut = new TriggerOcrCommandHandler(
            _captures, _storage, _watermark, _ocr, _ocrResults, _reviews, _auditLog, _settings, _secEvents, _alerts, _encryption);
    }

    private static Capture MakeTamperedCapture()
    {
        var capture = Capture.Create(ExamId.New(), StudentId.New(), DeviceId.New(),
            new PageNumber(1), Hash.FromHex(new string('a', 64)), new Signature(new byte[64]));
        capture.RecordUpload("storage/key/image.jpg");
        capture.FlagAsTampered("flagged by admin");
        return capture;
    }

    [Fact]
    public async Task Handle_TamperedCapture_ThrowsCaptureAlreadyTamperedException()
    {
        var capture = MakeTamperedCapture();
        _captures.GetByIdAsync(Arg.Any<CaptureId>(), default).Returns(capture);

        var act = () => _sut.Handle(new TriggerOcrCommand(capture.Id.Value), default);

        await act.Should().ThrowAsync<CaptureAlreadyTamperedException>();
    }

    [Fact]
    public async Task Handle_TamperedCapture_NeverCallsOcrService()
    {
        var capture = MakeTamperedCapture();
        _captures.GetByIdAsync(Arg.Any<CaptureId>(), default).Returns(capture);

        try { await _sut.Handle(new TriggerOcrCommand(capture.Id.Value), default); } catch { }

        await _ocr.DidNotReceive().ExtractAsync(Arg.Any<byte[]>(), default);
        await _ocrResults.DidNotReceive().AddAsync(Arg.Any<OcrResult>(), default);
    }
}
