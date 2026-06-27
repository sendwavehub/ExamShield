using ExamShield.Application.Commands.VerifyIntegrity;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.Services;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.VerifyIntegrity;

public sealed class VerifyIntegrityReadOnlyTests
{
    private readonly ICaptureRepository      _captures   = Substitute.For<ICaptureRepository>();
    private readonly HashVerificationService  _hasher     = new();
    private readonly IAuditLogRepository     _auditLog   = Substitute.For<IAuditLogRepository>();
    private readonly IAlertService           _alerts     = Substitute.For<IAlertService>();
    private readonly VerifyIntegrityCommandHandler _sut;

    public VerifyIntegrityReadOnlyTests()
    {
        _sut = new VerifyIntegrityCommandHandler(_captures, _hasher, _auditLog, _alerts);
    }

    private static Capture MakeUploadedCapture(string hashHex)
    {
        var capture = Capture.Create(
            new ExamId(Guid.NewGuid()), new StudentId(Guid.NewGuid()),
            new DeviceId(Guid.NewGuid()), new PageNumber(1),
            Hash.FromHex(hashHex), new Signature(new byte[64]));
        capture.RecordUpload("storage-key");
        return capture;
    }

    [Fact]
    public async Task Handle_MismatchedBytes_DoesNotPersistTamperedStatus()
    {
        var realHash  = new string('a', 64);
        var capture   = MakeUploadedCapture(realHash);
        var statusBefore = capture.Status;

        _captures.GetByIdAsync(Arg.Any<CaptureId>(), default).Returns(capture);

        // Send bytes whose hash does NOT match the stored expected hash
        var badBytes = new byte[] { 0xFF, 0xFE };
        await _sut.Handle(new VerifyIntegrityCommand(capture.Id.Value, badBytes), default);

        // The capture must NOT be persisted with Tampered status
        await _captures.DidNotReceive().UpdateAsync(Arg.Any<Capture>(), default);
    }

    [Fact]
    public async Task Handle_MismatchedBytes_ReturnsFalse()
    {
        var realHash = new string('a', 64);
        var capture  = MakeUploadedCapture(realHash);
        _captures.GetByIdAsync(Arg.Any<CaptureId>(), default).Returns(capture);

        var result = await _sut.Handle(new VerifyIntegrityCommand(capture.Id.Value, new byte[] { 0xFF }), default);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CorrectBytes_ReturnsTrue()
    {
        // Use 32 zero bytes; SHA-256 of that is deterministic
        var imageBytes = new byte[32];
        var actualHash = new HashVerificationService().ComputeHash(imageBytes).Hex;
        var capture    = MakeUploadedCapture(actualHash);
        _captures.GetByIdAsync(Arg.Any<CaptureId>(), default).Returns(capture);

        var result = await _sut.Handle(new VerifyIntegrityCommand(capture.Id.Value, imageBytes), default);

        result.IsValid.Should().BeTrue();
        await _captures.DidNotReceive().UpdateAsync(Arg.Any<Capture>(), default);
    }
}
