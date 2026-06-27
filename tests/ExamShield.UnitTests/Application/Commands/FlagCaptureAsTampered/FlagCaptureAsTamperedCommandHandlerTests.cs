using ExamShield.Application.Commands.FlagCaptureAsTampered;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.FlagCaptureAsTampered;

public sealed class FlagCaptureAsTamperedCommandHandlerTests
{
    private readonly ICaptureRepository  _repo    = Substitute.For<ICaptureRepository>();
    private readonly IAuditLogRepository _audit   = Substitute.For<IAuditLogRepository>();
    private readonly IAlertService       _alerts  = Substitute.For<IAlertService>();
    private readonly FlagCaptureAsTamperedCommandHandler _sut;

    public FlagCaptureAsTamperedCommandHandlerTests() =>
        _sut = new FlagCaptureAsTamperedCommandHandler(_repo, _audit, _alerts);

    private static Capture MakeCapture(bool uploaded = false)
    {
        var hash = Hash.FromBytes(new byte[32]);
        var c = Capture.Create(ExamId.New(), StudentId.New(), DeviceId.New(),
            new PageNumber(1), hash, new Signature(new byte[64]));
        if (uploaded) c.RecordUpload("storage/key");
        return c;
    }

    [Fact]
    public async Task Handle_CaptureNotFound_ThrowsCaptureNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<CaptureId>(), Arg.Any<CancellationToken>())
            .Returns((Capture?)null);

        await Assert.ThrowsAsync<CaptureNotFoundException>(
            () => _sut.Handle(new FlagCaptureAsTamperedCommand(Guid.NewGuid(), "forged"), default));
    }

    [Fact]
    public async Task Handle_ValidCapture_SetsTamperedStatus()
    {
        var capture = MakeCapture(uploaded: true);
        _repo.GetByIdAsync(Arg.Any<CaptureId>(), Arg.Any<CancellationToken>()).Returns(capture);

        await _sut.Handle(new FlagCaptureAsTamperedCommand(capture.Id.Value, "altered ink"), default);

        Assert.Equal(CaptureStatus.Tampered, capture.Status);
    }

    [Fact]
    public async Task Handle_ValidCapture_PersistsCapture()
    {
        var capture = MakeCapture(uploaded: true);
        _repo.GetByIdAsync(Arg.Any<CaptureId>(), Arg.Any<CancellationToken>()).Returns(capture);

        await _sut.Handle(new FlagCaptureAsTamperedCommand(capture.Id.Value, "erased answers"), default);

        await _repo.Received(1).UpdateAsync(capture, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCapture_SendsTamperingAlert()
    {
        var capture = MakeCapture(uploaded: true);
        _repo.GetByIdAsync(Arg.Any<CaptureId>(), Arg.Any<CancellationToken>()).Returns(capture);

        await _sut.Handle(new FlagCaptureAsTamperedCommand(capture.Id.Value, "investigation finding"), default);

        await _alerts.Received(1).SendAsync(
            AlertType.TamperingDetected, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyTampered_ThrowsCaptureAlreadyTamperedException()
    {
        var capture = MakeCapture();
        capture.FlagAsTampered("prior detection");
        _repo.GetByIdAsync(Arg.Any<CaptureId>(), Arg.Any<CancellationToken>()).Returns(capture);

        await Assert.ThrowsAsync<CaptureAlreadyTamperedException>(
            () => _sut.Handle(new FlagCaptureAsTamperedCommand(capture.Id.Value, "again"), default));
    }
}
