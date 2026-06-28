using ExamShield.Domain.ValueObjects;
using FluentAssertions;

namespace ExamShield.UnitTests.Domain.ValueObjects;

public sealed class GuidIdTests
{
    // ── GuidId base invariants via each concrete subtype ──────────────────────

    [Theory]
    [InlineData("AuditLogId")]
    [InlineData("ManualReviewId")]
    [InlineData("OcrResultId")]
    [InlineData("ReviewRequestId")]
    [InlineData("ScoreId")]
    [InlineData("UserId")]
    public void Constructor_EmptyGuid_ThrowsArgumentException(string typeName)
    {
        Action act = typeName switch
        {
            "AuditLogId"       => () => _ = new AuditLogId(Guid.Empty),
            "ManualReviewId"   => () => _ = new ManualReviewId(Guid.Empty),
            "OcrResultId"      => () => _ = new OcrResultId(Guid.Empty),
            "ReviewRequestId"  => () => _ = new ReviewRequestId(Guid.Empty),
            "ScoreId"          => () => _ = new ScoreId(Guid.Empty),
            "UserId"           => () => _ = new UserId(Guid.Empty),
            _ => throw new ArgumentOutOfRangeException()
        };
        act.Should().Throw<ArgumentException>();
    }

    // ── AuditLogId ────────────────────────────────────────────────────────────

    [Fact] public void AuditLogId_Constructor_StoresValue()
    {
        var g = Guid.NewGuid(); new AuditLogId(g).Value.Should().Be(g);
    }
    [Fact] public void AuditLogId_New_ProducesUniqueValues()
    {
        AuditLogId.New().Should().NotBe(AuditLogId.New());
    }
    [Fact] public void AuditLogId_RecordEquality_SameValue_AreEqual()
    {
        var g = Guid.NewGuid(); new AuditLogId(g).Should().Be(new AuditLogId(g));
    }

    // ── ManualReviewId ────────────────────────────────────────────────────────

    [Fact] public void ManualReviewId_Constructor_StoresValue()
    {
        var g = Guid.NewGuid(); new ManualReviewId(g).Value.Should().Be(g);
    }
    [Fact] public void ManualReviewId_New_ProducesUniqueValues()
    {
        ManualReviewId.New().Should().NotBe(ManualReviewId.New());
    }
    [Fact] public void ManualReviewId_RecordEquality_SameValue_AreEqual()
    {
        var g = Guid.NewGuid(); new ManualReviewId(g).Should().Be(new ManualReviewId(g));
    }

    // ── OcrResultId ───────────────────────────────────────────────────────────

    [Fact] public void OcrResultId_Constructor_StoresValue()
    {
        var g = Guid.NewGuid(); new OcrResultId(g).Value.Should().Be(g);
    }
    [Fact] public void OcrResultId_New_ProducesUniqueValues()
    {
        OcrResultId.New().Should().NotBe(OcrResultId.New());
    }
    [Fact] public void OcrResultId_RecordEquality_SameValue_AreEqual()
    {
        var g = Guid.NewGuid(); new OcrResultId(g).Should().Be(new OcrResultId(g));
    }

    // ── ReviewRequestId ───────────────────────────────────────────────────────

    [Fact] public void ReviewRequestId_Constructor_StoresValue()
    {
        var g = Guid.NewGuid(); new ReviewRequestId(g).Value.Should().Be(g);
    }
    [Fact] public void ReviewRequestId_New_ProducesUniqueValues()
    {
        ReviewRequestId.New().Should().NotBe(ReviewRequestId.New());
    }
    [Fact] public void ReviewRequestId_RecordEquality_SameValue_AreEqual()
    {
        var g = Guid.NewGuid(); new ReviewRequestId(g).Should().Be(new ReviewRequestId(g));
    }

    // ── ScoreId ───────────────────────────────────────────────────────────────

    [Fact] public void ScoreId_Constructor_StoresValue()
    {
        var g = Guid.NewGuid(); new ScoreId(g).Value.Should().Be(g);
    }
    [Fact] public void ScoreId_New_ProducesUniqueValues()
    {
        ScoreId.New().Should().NotBe(ScoreId.New());
    }
    [Fact] public void ScoreId_RecordEquality_SameValue_AreEqual()
    {
        var g = Guid.NewGuid(); new ScoreId(g).Should().Be(new ScoreId(g));
    }

    // ── UserId ────────────────────────────────────────────────────────────────

    [Fact] public void UserId_Constructor_StoresValue()
    {
        var g = Guid.NewGuid(); new UserId(g).Value.Should().Be(g);
    }
    [Fact] public void UserId_New_ProducesUniqueValues()
    {
        UserId.New().Should().NotBe(UserId.New());
    }
    [Fact] public void UserId_RecordEquality_SameValue_AreEqual()
    {
        var g = Guid.NewGuid(); new UserId(g).Should().Be(new UserId(g));
    }
}
