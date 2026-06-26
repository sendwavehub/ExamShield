using System.Security.Cryptography;
using ExamShield.Application.Commands.UploadImage;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.Services;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands;

public sealed class UploadImageCommandHandlerTests
{
    private readonly ICaptureRepository _repository = Substitute.For<ICaptureRepository>();
    private readonly IImageStorage _imageStorage = Substitute.For<IImageStorage>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly HashVerificationService _hashService = new();
    private readonly UploadImageCommandHandler _sut;

    private static readonly byte[] SampleImage = "answer-sheet-bytes"u8.ToArray();
    private static readonly Hash SampleHash = Hash.FromBytes(SHA256.HashData(SampleImage));

    public UploadImageCommandHandlerTests()
    {
        _imageStorage.StoreAsync(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns("captures/test-key");

        _sut = new UploadImageCommandHandler(_repository, _hashService, _imageStorage, _auditLog);
    }

    private Capture CaptureWithSampleHash()
    {
        var capture = Capture.Create(
            ExamId.New(), StudentId.New(), DeviceId.New(),
            new PageNumber(1), SampleHash, new Signature(new byte[64]));
        _repository.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        return capture;
    }

    [Fact]
    public async Task Handle_WithValidImageAndMatchingHash_ReturnsStorageKey()
    {
        var capture = CaptureWithSampleHash();
        var command = new UploadImageCommand(capture.Id.Value, SampleImage);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.StorageKey.Should().Be("captures/test-key");
    }

    [Fact]
    public async Task Handle_WithValidImageAndMatchingHash_StoresImage()
    {
        var capture = CaptureWithSampleHash();

        await _sut.Handle(new UploadImageCommand(capture.Id.Value, SampleImage), CancellationToken.None);

        await _imageStorage.Received(1).StoreAsync(capture.Id.Value, SampleImage, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidImageAndMatchingHash_PersistsCaptureAsUploaded()
    {
        var capture = CaptureWithSampleHash();

        await _sut.Handle(new UploadImageCommand(capture.Id.Value, SampleImage), CancellationToken.None);

        await _repository.Received(1).UpdateAsync(
            Arg.Is<Capture>(c => c.Status == CaptureStatus.Uploaded),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidImageAndMatchingHash_AppendsImageUploadedAuditEntry()
    {
        var capture = CaptureWithSampleHash();

        await _sut.Handle(new UploadImageCommand(capture.Id.Value, SampleImage), CancellationToken.None);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(e => e.Action == AuditAction.ImageUploaded && e.CaptureId == capture.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCaptureNotFound_ThrowsCaptureNotFoundException()
    {
        _repository.GetByIdAsync(Arg.Any<CaptureId>(), Arg.Any<CancellationToken>()).Returns((Capture?)null);

        var act = () => _sut.Handle(new UploadImageCommand(Guid.NewGuid(), SampleImage), CancellationToken.None);

        await act.Should().ThrowAsync<CaptureNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenHashMismatches_ThrowsHashMismatchException()
    {
        var capture = Capture.Create(
            ExamId.New(), StudentId.New(), DeviceId.New(),
            new PageNumber(1), Hash.FromHex(new string('b', 64)), new Signature(new byte[64]));
        _repository.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);

        var act = () => _sut.Handle(new UploadImageCommand(capture.Id.Value, SampleImage), CancellationToken.None);

        await act.Should().ThrowAsync<HashMismatchException>();
    }

    [Fact]
    public async Task Handle_WhenHashMismatches_DoesNotStoreImage()
    {
        var capture = Capture.Create(
            ExamId.New(), StudentId.New(), DeviceId.New(),
            new PageNumber(1), Hash.FromHex(new string('b', 64)), new Signature(new byte[64]));
        _repository.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);

        try { await _sut.Handle(new UploadImageCommand(capture.Id.Value, SampleImage), CancellationToken.None); }
        catch (HashMismatchException) { }

        await _imageStorage.DidNotReceive().StoreAsync(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyImageBytes_ThrowsArgumentException()
    {
        var act = () => _sut.Handle(new UploadImageCommand(Guid.NewGuid(), Array.Empty<byte>()), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenAlreadyUploaded_ThrowsDuplicateUploadException()
    {
        var capture = CaptureWithSampleHash();
        capture.RecordUpload("captures/existing-key");

        var act = () => _sut.Handle(new UploadImageCommand(capture.Id.Value, SampleImage), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateUploadException>();
    }
}
