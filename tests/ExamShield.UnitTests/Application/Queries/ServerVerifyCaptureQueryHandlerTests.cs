using System.Security.Cryptography;
using ExamShield.Application.Queries.ServerVerifyCapture;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.Services;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ExamShield.UnitTests.Application.Queries;

public sealed class ServerVerifyCaptureQueryHandlerTests
{
    private readonly ICaptureRepository _captures = Substitute.For<ICaptureRepository>();
    private readonly IImageStorage _imageStorage = Substitute.For<IImageStorage>();
    private readonly IDeviceRepository _devices = Substitute.For<IDeviceRepository>();
    private readonly ISignatureVerificationService _sigService = Substitute.For<ISignatureVerificationService>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly IAlertService _alertService = Substitute.For<IAlertService>();
    private readonly IWatermarkService _watermarkService = Substitute.For<IWatermarkService>();
    private readonly IImageEncryptionService _encryption = Substitute.For<IImageEncryptionService>();
    private readonly HashVerificationService _hashService = new();
    private readonly ServerVerifyCaptureQueryHandler _sut;

    private static readonly byte[] ImageBytes = "exam-image"u8.ToArray();
    private static readonly string HashHex =
        Convert.ToHexString(SHA256.HashData(ImageBytes)).ToLowerInvariant();

    // Simulate what storage returns: watermarked bytes (original + appended envelope)
    private static readonly byte[] StoredBytes = [..ImageBytes, 0xAA, 0xBB, 0xCC];

    private WatermarkExtractionResult ValidExtraction => WatermarkExtractionResult.Success(
        new WatermarkPayload { ImageHash = HashHex },
        originalImageLength: ImageBytes.Length);

    public ServerVerifyCaptureQueryHandlerTests()
    {
        _sut = new ServerVerifyCaptureQueryHandler(
            _captures, _imageStorage, _hashService, _devices, _sigService, _auditLog, _alertService,
            _watermarkService, _encryption);
    }

    private Capture BuildUploadedCapture()
    {
        var capture = Capture.Create(
            new ExamId(Guid.NewGuid()), new StudentId(Guid.NewGuid()),
            new DeviceId(Guid.NewGuid()), new PageNumber(1),
            Hash.FromHex(HashHex), new Signature([0x01, 0x02]));
        capture.RecordUpload("captures/test");
        _captures.GetByIdAsync(Arg.Any<CaptureId>(), Arg.Any<CancellationToken>()).Returns(capture);
        _imageStorage.RetrieveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(StoredBytes);
        _watermarkService.Extract(StoredBytes).Returns(ValidExtraction);
        return capture;
    }

    [Fact]
    public async Task Handle_WhenCaptureNotFound_ThrowsCaptureNotFoundException()
    {
        _captures.GetByIdAsync(Arg.Any<CaptureId>(), Arg.Any<CancellationToken>())
            .Returns((Capture?)null);

        var act = () => _sut.Handle(new ServerVerifyCaptureQuery(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<CaptureNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCaptureNotUploaded_ThrowsCaptureNotUploadedException()
    {
        var capture = Capture.Create(
            new ExamId(Guid.NewGuid()), new StudentId(Guid.NewGuid()),
            new DeviceId(Guid.NewGuid()), new PageNumber(1),
            Hash.FromHex(HashHex), new Signature([0x01]));
        _captures.GetByIdAsync(Arg.Any<CaptureId>(), Arg.Any<CancellationToken>()).Returns(capture);

        var act = () => _sut.Handle(new ServerVerifyCaptureQuery(capture.Id.Value), default);

        await act.Should().ThrowAsync<CaptureNotUploadedException>();
    }

    [Fact]
    public async Task Handle_WhenHashMatchesAndSignatureValid_ReturnsIsValidTrue()
    {
        var capture = BuildUploadedCapture();
        var device = Device.Register("dev", new PublicKey([0x04]));
        _devices.GetByIdAsync(Arg.Any<DeviceId>(), Arg.Any<CancellationToken>()).Returns(device);
        _sigService.Verify(Arg.Any<Hash>(), Arg.Any<Signature>(), Arg.Any<PublicKey>()).Returns(true);

        var result = await _sut.Handle(new ServerVerifyCaptureQuery(capture.Id.Value), default);

        result.IsValid.Should().BeTrue();
        result.HashValid.Should().BeTrue();
        result.SignatureValid.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWatermarkExtractionFails_ReturnsHashInvalid()
    {
        var capture = BuildUploadedCapture();
        var device = Device.Register("dev", new PublicKey([0x04]));
        _devices.GetByIdAsync(Arg.Any<DeviceId>(), Arg.Any<CancellationToken>()).Returns(device);
        _sigService.Verify(Arg.Any<Hash>(), Arg.Any<Signature>(), Arg.Any<PublicKey>()).Returns(true);
        _watermarkService.Extract(Arg.Any<byte[]>()).Returns(WatermarkExtractionResult.Failure());

        var result = await _sut.Handle(new ServerVerifyCaptureQuery(capture.Id.Value), default);

        result.IsValid.Should().BeFalse();
        result.HashValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenSignatureInvalid_ReturnsIsValidFalse()
    {
        var capture = BuildUploadedCapture();
        var device = Device.Register("dev", new PublicKey([0x04]));
        _devices.GetByIdAsync(Arg.Any<DeviceId>(), Arg.Any<CancellationToken>()).Returns(device);
        _sigService.Verify(Arg.Any<Hash>(), Arg.Any<Signature>(), Arg.Any<PublicKey>()).Returns(false);

        var result = await _sut.Handle(new ServerVerifyCaptureQuery(capture.Id.Value), default);

        result.IsValid.Should().BeFalse();
        result.SignatureValid.Should().BeFalse();
    }
}
