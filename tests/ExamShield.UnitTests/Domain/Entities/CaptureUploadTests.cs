using ExamShield.Domain.Entities;
using ExamShield.Domain.Events;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class CaptureUploadTests
{
    private static readonly Hash ValidHash = Hash.FromHex(new string('a', 64));
    private static readonly Signature ValidSignature = new(new byte[64]);

    private static Capture MakeCapture() =>
        Capture.Create(ExamId.New(), StudentId.New(), DeviceId.New(),
            new PageNumber(1), ValidHash, ValidSignature);

    [Fact]
    public void RecordUpload_WithValidKey_SetsStatusToUploaded()
    {
        var capture = MakeCapture();

        capture.RecordUpload("captures/some-key");

        capture.Status.Should().Be(CaptureStatus.Uploaded);
    }

    [Fact]
    public void RecordUpload_StoresStorageKey()
    {
        var capture = MakeCapture();

        capture.RecordUpload("captures/abc");

        capture.StorageKey.Should().Be("captures/abc");
    }

    [Fact]
    public void RecordUpload_RaisesImageUploadedEvent()
    {
        var capture = MakeCapture();

        capture.RecordUpload("captures/abc");

        capture.DomainEvents.OfType<ImageUploaded>()
            .Should().ContainSingle()
            .Which.CaptureId.Should().Be(capture.Id);
    }

    [Fact]
    public void RecordUpload_WhenAlreadyUploaded_ThrowsDuplicateUploadException()
    {
        var capture = MakeCapture();
        capture.RecordUpload("captures/first");

        var act = () => capture.RecordUpload("captures/second");

        act.Should().Throw<DuplicateUploadException>();
    }

    [Fact]
    public void RecordUpload_WithNullKey_ThrowsArgumentException()
    {
        var capture = MakeCapture();

        var act = () => capture.RecordUpload(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordUpload_WithEmptyKey_ThrowsArgumentException()
    {
        var capture = MakeCapture();

        var act = () => capture.RecordUpload("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordUpload_WhenTampered_ThrowsDuplicateUploadException()
    {
        var capture = MakeCapture();
        capture.VerifyIntegrity(Hash.FromHex(new string('b', 64))); // → Tampered

        var act = () => capture.RecordUpload("captures/key");

        act.Should().Throw<DuplicateUploadException>();
    }
}
