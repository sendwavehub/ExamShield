using ExamShield.Application.Commands.TriggerOcr;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands;

public sealed class PreventDuplicateOcrTests
{
    private readonly ICaptureRepository       _captures      = Substitute.For<ICaptureRepository>();
    private readonly IImageStorage            _imageStorage  = Substitute.For<IImageStorage>();
    private readonly IWatermarkService        _watermark     = Substitute.For<IWatermarkService>();
    private readonly IOcrService              _ocrService    = Substitute.For<IOcrService>();
    private readonly IOcrResultRepository     _ocrResults    = Substitute.For<IOcrResultRepository>();
    private readonly IManualReviewRepository  _manualReviews = Substitute.For<IManualReviewRepository>();
    private readonly IAuditLogRepository      _auditLog      = Substitute.For<IAuditLogRepository>();
    private readonly ISystemSettingsRepository _settings     = Substitute.For<ISystemSettingsRepository>();
    private readonly ISecurityEventRepository _secEvents     = Substitute.For<ISecurityEventRepository>();
    private readonly IAlertService            _alerts        = Substitute.For<IAlertService>();
    private readonly TriggerOcrCommandHandler _sut;

    public PreventDuplicateOcrTests()
    {
        _settings.GetAsync(Arg.Any<CancellationToken>())
                 .Returns(SystemSettings.CreateDefault());
        _sut = new TriggerOcrCommandHandler(
            _captures, _imageStorage, _watermark, _ocrService,
            _ocrResults, _manualReviews, _auditLog, _settings, _secEvents, _alerts);
    }

    private static Capture MakeUploadedCapture()
    {
        var c = Capture.Create(
            ExamId.New(), StudentId.New(), DeviceId.New(),
            new PageNumber(1), Hash.FromBytes(new byte[32]), new Signature(new byte[64]));
        c.RecordUpload("key.jpg");
        return c;
    }

    private static OcrResult MakeExistingResult(CaptureId captureId) =>
        OcrResult.Create(captureId, [new ExtractedAnswer(1, "A", new OcrConfidence(0.95))]);

    [Fact]
    public async Task Handle_WhenOcrResultAlreadyExists_ThrowsDuplicateOcrException()
    {
        var capture = MakeUploadedCapture();
        _captures.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _ocrResults.GetByCaptureIdAsync(capture.Id, Arg.Any<CancellationToken>())
                   .Returns(MakeExistingResult(capture.Id));

        var act = () => _sut.Handle(new TriggerOcrCommand(capture.Id.Value), default);

        await act.Should().ThrowAsync<DuplicateOcrException>();
    }

    [Fact]
    public async Task Handle_WhenOcrResultAlreadyExists_DoesNotRunOcr()
    {
        var capture = MakeUploadedCapture();
        _captures.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _ocrResults.GetByCaptureIdAsync(capture.Id, Arg.Any<CancellationToken>())
                   .Returns(MakeExistingResult(capture.Id));

        try { await _sut.Handle(new TriggerOcrCommand(capture.Id.Value), default); } catch { }

        await _ocrService.DidNotReceive().ExtractAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoExistingResult_RunsOcr()
    {
        var capture = MakeUploadedCapture();
        _captures.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _ocrResults.GetByCaptureIdAsync(capture.Id, Arg.Any<CancellationToken>())
                   .Returns((OcrResult?)null);

        var bytes = new byte[200];
        _imageStorage.RetrieveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(bytes);
        _watermark.Extract(bytes).Returns(WatermarkExtractionResult.Success(
            new WatermarkPayload { CaptureId = capture.Id.Value, ExamId = capture.ExamId.Value }, 100));
        _ocrService.ExtractAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
                   .Returns(new OcrExtractionResult([]));

        await _sut.Handle(new TriggerOcrCommand(capture.Id.Value), default);

        await _ocrService.Received(1).ExtractAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }
}
