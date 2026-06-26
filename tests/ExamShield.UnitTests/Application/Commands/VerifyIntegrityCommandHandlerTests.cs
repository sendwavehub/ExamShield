using System.Security.Cryptography;
using ExamShield.Application.Commands.VerifyIntegrity;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.Services;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands;

public sealed class VerifyIntegrityCommandHandlerTests
{
    private readonly ICaptureRepository _repository = Substitute.For<ICaptureRepository>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly IAlertService _alertService = Substitute.For<IAlertService>();
    private readonly HashVerificationService _hashService = new();
    private readonly VerifyIntegrityCommandHandler _sut;

    public VerifyIntegrityCommandHandlerTests() =>
        _sut = new VerifyIntegrityCommandHandler(_repository, _hashService, _auditLog, _alertService);

    private static readonly byte[] SampleImage = "answer-sheet-bytes"u8.ToArray();

    private static Capture CaptureWithHash(Hash hash) =>
        Capture.Create(ExamId.New(), StudentId.New(), DeviceId.New(), new PageNumber(1), hash, new Signature(new byte[64]));

    [Fact]
    public async Task Handle_WhenHashMatches_ReturnsValidResult()
    {
        var correctHash = Hash.FromBytes(SHA256.HashData(SampleImage));
        var capture = CaptureWithHash(correctHash);
        _repository.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);

        var result = await _sut.Handle(new VerifyIntegrityCommand(capture.Id.Value, SampleImage), default);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenHashMatches_ResultContainsMatchingHashes()
    {
        var correctHash = Hash.FromBytes(SHA256.HashData(SampleImage));
        var capture = CaptureWithHash(correctHash);
        _repository.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);

        var result = await _sut.Handle(new VerifyIntegrityCommand(capture.Id.Value, SampleImage), default);

        result.ExpectedHash.Should().Be(result.ActualHash);
    }

    [Fact]
    public async Task Handle_WhenHashMismatches_ReturnsInvalidResult()
    {
        var capture = CaptureWithHash(Hash.FromHex(new string('b', 64)));
        _repository.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);

        var result = await _sut.Handle(new VerifyIntegrityCommand(capture.Id.Value, SampleImage), default);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenHashMismatches_ResultContainsDifferentHashes()
    {
        var capture = CaptureWithHash(Hash.FromHex(new string('b', 64)));
        _repository.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);

        var result = await _sut.Handle(new VerifyIntegrityCommand(capture.Id.Value, SampleImage), default);

        result.ExpectedHash.Should().NotBe(result.ActualHash);
    }

    [Fact]
    public async Task Handle_WhenCaptureNotFound_ThrowsCaptureNotFoundException()
    {
        _repository.GetByIdAsync(Arg.Any<CaptureId>(), Arg.Any<CancellationToken>()).Returns((Capture?)null);

        await Assert.ThrowsAsync<CaptureNotFoundException>(() =>
            _sut.Handle(new VerifyIntegrityCommand(Guid.NewGuid(), SampleImage), default));
    }

    [Fact]
    public async Task Handle_WithEmptyImageBytes_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.Handle(new VerifyIntegrityCommand(Guid.NewGuid(), Array.Empty<byte>()), default));
    }

    [Fact]
    public async Task Handle_AfterVerification_PersistsCaptureStatusChange()
    {
        var correctHash = Hash.FromBytes(SHA256.HashData(SampleImage));
        var capture = CaptureWithHash(correctHash);
        _repository.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);

        await _sut.Handle(new VerifyIntegrityCommand(capture.Id.Value, SampleImage), default);

        await _repository.Received(1).UpdateAsync(
            Arg.Is<Capture>(c => c.Status == CaptureStatus.Verified), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHashMatches_AppendsHashVerifiedAuditEntry()
    {
        var correctHash = Hash.FromBytes(SHA256.HashData(SampleImage));
        var capture = CaptureWithHash(correctHash);
        _repository.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);

        await _sut.Handle(new VerifyIntegrityCommand(capture.Id.Value, SampleImage), default);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(e => e.Action == AuditAction.HashVerified && e.CaptureId == capture.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHashMismatches_AppendsTamperingDetectedAuditEntry()
    {
        var capture = CaptureWithHash(Hash.FromHex(new string('b', 64)));
        _repository.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);

        await _sut.Handle(new VerifyIntegrityCommand(capture.Id.Value, SampleImage), default);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(e => e.Action == AuditAction.TamperingDetected && e.CaptureId == capture.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTamperingDetected_SendsTamperingAlert()
    {
        var capture = CaptureWithHash(Hash.FromHex(new string('b', 64)));
        _repository.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);

        await _sut.Handle(new VerifyIntegrityCommand(capture.Id.Value, SampleImage), default);

        await _alertService.Received(1).SendAsync(
            AlertType.TamperingDetected, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHashMatches_NoAlertSent()
    {
        var correctHash = Hash.FromBytes(SHA256.HashData(SampleImage));
        var capture = CaptureWithHash(correctHash);
        _repository.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);

        await _sut.Handle(new VerifyIntegrityCommand(capture.Id.Value, SampleImage), default);

        await _alertService.DidNotReceive().SendAsync(
            Arg.Any<AlertType>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
