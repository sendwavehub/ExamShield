using ExamShield.Application.Commands.RejectReview;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.RejectReview;

public sealed class RejectReviewAuditTests
{
    private readonly IManualReviewRepository _reviews  = Substitute.For<IManualReviewRepository>();
    private readonly IAuditLogRepository     _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly RejectReviewCommandHandler _sut;

    public RejectReviewAuditTests() =>
        _sut = new RejectReviewCommandHandler(_reviews, _auditLog);

    private static ManualReview MakeCompletedReview()
    {
        var captureId = new CaptureId(Guid.NewGuid());
        var ocr = OcrResult.Create(captureId,
            [new ExtractedAnswer(1, "A", new OcrConfidence(0.99))]);
        var review = ManualReview.CreateFor(ocr);
        review.Complete([new ReviewedAnswer(1, "A")], new UserId(Guid.NewGuid()));
        return review;
    }

    [Fact]
    public async Task Handle_Rejection_AppendsReviewRejectedAuditEntry()
    {
        var review = MakeCompletedReview();
        _reviews.GetByIdAsync(Arg.Any<ManualReviewId>(), default).Returns(review);

        await _sut.Handle(
            new RejectReviewCommand(Guid.NewGuid(), Guid.NewGuid(), "Illegible answer"), default);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.ReviewRejected), default);
    }
}
