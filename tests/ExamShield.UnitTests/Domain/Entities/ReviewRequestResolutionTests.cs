using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.UnitTests.Domain.Entities;

public sealed class ReviewRequestResolutionTests
{
    private static ReviewRequest Pending() =>
        ReviewRequest.Submit(StudentId.New(), CaptureId.New(), "My sheet was marked wrong.");

    [Fact]
    public void Resolve_PendingRequest_SetsStatusResolved()
    {
        var rr = Pending();
        rr.Resolve("Score corrected after review.");
        Assert.Equal(ReviewRequestStatus.Resolved, rr.Status);
    }

    [Fact]
    public void Resolve_SetsResolutionNote()
    {
        var rr = Pending();
        rr.Resolve("Score corrected after review.");
        Assert.Equal("Score corrected after review.", rr.ResolutionNote);
    }

    [Fact]
    public void Reject_PendingRequest_SetsStatusRejected()
    {
        var rr = Pending();
        rr.Reject("No evidence of mismark found.");
        Assert.Equal(ReviewRequestStatus.Rejected, rr.Status);
    }

    [Fact]
    public void Reject_SetsResolutionNote()
    {
        var rr = Pending();
        rr.Reject("No evidence of mismark found.");
        Assert.Equal("No evidence of mismark found.", rr.ResolutionNote);
    }

    [Fact]
    public void Resolve_EmptyNote_Throws()
    {
        var rr = Pending();
        Assert.Throws<ArgumentException>(() => rr.Resolve("   "));
    }

    [Fact]
    public void Reject_EmptyReason_Throws()
    {
        var rr = Pending();
        Assert.Throws<ArgumentException>(() => rr.Reject(""));
    }

    [Fact]
    public void Resolve_AlreadyResolved_ThrowsInvalidOperation()
    {
        var rr = Pending();
        rr.Resolve("First resolution.");
        Assert.Throws<InvalidOperationException>(() => rr.Resolve("Second resolution."));
    }

    [Fact]
    public void Reject_AlreadyRejected_ThrowsInvalidOperation()
    {
        var rr = Pending();
        rr.Reject("Not valid.");
        Assert.Throws<InvalidOperationException>(() => rr.Reject("Again."));
    }
}
