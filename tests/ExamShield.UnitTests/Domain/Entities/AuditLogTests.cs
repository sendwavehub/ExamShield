using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class AuditLogTests
{
    [Fact]
    public void Record_CreatesEntryWithCorrectAction()
    {
        var entry = AuditLog.Record(AuditAction.CaptureRegistered);

        entry.Action.Should().Be(AuditAction.CaptureRegistered);
    }

    [Fact]
    public void Record_AssignsNonEmptyId()
    {
        var entry = AuditLog.Record(AuditAction.CaptureRegistered);

        entry.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Record_WithCaptureId_StoresCaptureId()
    {
        var captureId = CaptureId.New();

        var entry = AuditLog.Record(AuditAction.CaptureRegistered, captureId: captureId);

        entry.CaptureId.Should().Be(captureId);
    }

    [Fact]
    public void Record_WithoutCaptureId_CaptureIdIsNull()
    {
        var entry = AuditLog.Record(AuditAction.CaptureRegistered);

        entry.CaptureId.Should().BeNull();
    }

    [Fact]
    public void Record_SetsOccurredAtToNow()
    {
        var before = DateTimeOffset.UtcNow;

        var entry = AuditLog.Record(AuditAction.CaptureRegistered);

        entry.OccurredAt.Should().BeOnOrAfter(before);
        entry.OccurredAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Record_DefaultsUserIdToSystem()
    {
        var entry = AuditLog.Record(AuditAction.CaptureRegistered);

        entry.UserId.Should().Be("system");
    }

    [Fact]
    public void Record_DefaultsIpAddressToUnknown()
    {
        var entry = AuditLog.Record(AuditAction.CaptureRegistered);

        entry.IpAddress.Should().Be("unknown");
    }

    [Fact]
    public void Record_WithReason_StoresReason()
    {
        var entry = AuditLog.Record(AuditAction.TamperingDetected, reason: "hash mismatch");

        entry.Reason.Should().Be("hash mismatch");
    }

    [Fact]
    public void Record_WithoutReason_ReasonIsNull()
    {
        var entry = AuditLog.Record(AuditAction.CaptureRegistered);

        entry.Reason.Should().BeNull();
    }

    // ── SetChainHashes ────────────────────────────────────────────────────────

    [Fact]
    public void SetChainHashes_StoresPreviousHashAndContentHash()
    {
        var entry = AuditLog.Record(AuditAction.CaptureRegistered);

        entry.SetChainHashes("prev-hash-hex", "content-hash-hex");

        entry.PreviousHash.Should().Be("prev-hash-hex");
        entry.ContentHash.Should().Be("content-hash-hex");
    }

    [Fact]
    public void SetChainHashes_CanBeCalledWithEmptyStrings()
    {
        var entry = AuditLog.Record(AuditAction.CaptureRegistered);

        var act = () => entry.SetChainHashes(string.Empty, string.Empty);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetChainHashes_SecondCallOverwritesPrevious()
    {
        var entry = AuditLog.Record(AuditAction.CaptureRegistered);
        entry.SetChainHashes("first-prev", "first-content");

        entry.SetChainHashes("second-prev", "second-content");

        entry.PreviousHash.Should().Be("second-prev");
        entry.ContentHash.Should().Be("second-content");
    }

    // ── SetServerSignature ────────────────────────────────────────────────────

    [Fact]
    public void SetServerSignature_StoresSignature()
    {
        var entry = AuditLog.Record(AuditAction.CaptureRegistered);

        entry.SetServerSignature("sig-hex-value");

        entry.ServerSignature.Should().Be("sig-hex-value");
    }

    [Fact]
    public void SetServerSignature_DefaultsToEmpty()
    {
        var entry = AuditLog.Record(AuditAction.CaptureRegistered);

        entry.ServerSignature.Should().Be(string.Empty);
    }
}
