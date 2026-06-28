using ExamShield.Domain.Entities;
using ExamShield.Domain.Events;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class CaptureTests
{
    private static readonly Hash ValidHash = Hash.FromHex(new string('a', 64));
    private static readonly Signature ValidSignature = new(new byte[64]);

    private static Capture CreateValidCapture(Hash? hash = null) =>
        Capture.Create(
            ExamId.New(),
            StudentId.New(),
            DeviceId.New(),
            new PageNumber(1),
            hash ?? ValidHash,
            ValidSignature
        );

    [Fact]
    public void Create_WithValidArguments_RaisesCaptureCreatedEvent()
    {
        var capture = CreateValidCapture();

        capture.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CaptureCreated>();
    }

    [Fact]
    public void Create_SetsStatusToCreated()
    {
        var capture = CreateValidCapture();

        capture.Status.Should().Be(CaptureStatus.Created);
    }

    [Fact]
    public void Create_SetsCapturedAtToApproximatelyNow()
    {
        var before = DateTimeOffset.UtcNow;
        var capture = CreateValidCapture();
        var after = DateTimeOffset.UtcNow;

        capture.CapturedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_WithNullExamId_ThrowsArgumentNullException()
    {
        var act = () => Capture.Create(null!, StudentId.New(), DeviceId.New(), new PageNumber(1), ValidHash, ValidSignature);

        act.Should().Throw<ArgumentNullException>().WithParameterName("examId");
    }

    [Fact]
    public void Create_WithNullHash_ThrowsArgumentNullException()
    {
        var act = () => Capture.Create(ExamId.New(), StudentId.New(), DeviceId.New(), new PageNumber(1), null!, ValidSignature);

        act.Should().Throw<ArgumentNullException>().WithParameterName("hash");
    }

    [Fact]
    public void Create_WithNullSignature_ThrowsArgumentNullException()
    {
        var act = () => Capture.Create(ExamId.New(), StudentId.New(), DeviceId.New(), new PageNumber(1), ValidHash, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("signature");
    }

    [Fact]
    public void VerifyIntegrity_WhenHashMatches_ReturnsTrue()
    {
        var capture = CreateValidCapture();

        capture.VerifyIntegrity(ValidHash).Should().BeTrue();
    }

    [Fact]
    public void VerifyIntegrity_WhenHashMatches_SetsStatusToVerified()
    {
        var capture = CreateValidCapture();

        capture.VerifyIntegrity(ValidHash);

        capture.Status.Should().Be(CaptureStatus.Verified);
    }

    [Fact]
    public void VerifyIntegrity_WhenHashMismatches_ReturnsFalse()
    {
        var capture = CreateValidCapture();
        var differentHash = Hash.FromHex(new string('b', 64));

        capture.VerifyIntegrity(differentHash).Should().BeFalse();
    }

    [Fact]
    public void VerifyIntegrity_WhenHashMismatches_SetsStatusToTampered()
    {
        var capture = CreateValidCapture();

        capture.VerifyIntegrity(Hash.FromHex(new string('b', 64)));

        capture.Status.Should().Be(CaptureStatus.Tampered);
    }

    [Fact]
    public void VerifyIntegrity_WhenHashMismatches_RaisesTamperingDetectedEvent()
    {
        var capture = CreateValidCapture();

        capture.VerifyIntegrity(Hash.FromHex(new string('b', 64)));

        capture.DomainEvents.OfType<TamperingDetected>()
            .Should().ContainSingle()
            .Which.CaptureId.Should().Be(capture.Id);
    }

    [Fact]
    public void VerifyIntegrity_WithNullHash_ThrowsArgumentNullException()
    {
        var capture = CreateValidCapture();

        var act = () => capture.VerifyIntegrity(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("currentHash");
    }

    [Fact]
    public void TwoCaptures_AlwaysHaveDifferentIds()
    {
        CreateValidCapture().Id.Should().NotBe(CreateValidCapture().Id);
    }

    // ── RecordUpload ──────────────────────────────────────────────────────────

    [Fact]
    public void RecordUpload_ValidKey_SetsStorageKeyAndStatus()
    {
        var capture = CreateValidCapture();

        capture.RecordUpload("s3://bucket/cap-1");

        capture.StorageKey.Should().Be("s3://bucket/cap-1");
        capture.Status.Should().Be(CaptureStatus.Uploaded);
    }

    [Fact]
    public void RecordUpload_ValidKey_RaisesImageUploadedEvent()
    {
        var capture = CreateValidCapture();

        capture.RecordUpload("s3://bucket/cap-1");

        capture.DomainEvents.OfType<ImageUploaded>()
            .Should().ContainSingle()
            .Which.CaptureId.Should().Be(capture.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordUpload_EmptyKey_ThrowsArgumentException(string key)
    {
        var capture = CreateValidCapture();

        var act = () => capture.RecordUpload(key);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordUpload_WhenAlreadyUploaded_ThrowsDuplicateUploadException()
    {
        var capture = CreateValidCapture();
        capture.RecordUpload("s3://bucket/cap-1");

        var act = () => capture.RecordUpload("s3://bucket/cap-2");

        act.Should().Throw<ExamShield.Domain.Exceptions.DuplicateUploadException>();
    }

    // ── FlagAsTampered ────────────────────────────────────────────────────────

    [Fact]
    public void FlagAsTampered_SetsStatusToTampered()
    {
        var capture = CreateValidCapture();

        capture.FlagAsTampered("Manual review flagged mismatch");

        capture.Status.Should().Be(CaptureStatus.Tampered);
    }

    [Fact]
    public void FlagAsTampered_RaisesTamperingDetectedEvent()
    {
        var capture = CreateValidCapture();

        capture.FlagAsTampered("Reason");

        capture.DomainEvents.OfType<TamperingDetected>()
            .Should().ContainSingle()
            .Which.CaptureId.Should().Be(capture.Id);
    }

    [Fact]
    public void FlagAsTampered_WhenAlreadyTampered_ThrowsCaptureAlreadyTamperedException()
    {
        var capture = CreateValidCapture();
        capture.FlagAsTampered("First");

        var act = () => capture.FlagAsTampered("Second");

        act.Should().Throw<ExamShield.Domain.Exceptions.CaptureAlreadyTamperedException>();
    }
}
