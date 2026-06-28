using ExamShield.Application.Commands.ApproveReview;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.ApproveReview;

public sealed class ApproveReviewAuditTests
{
    private readonly IManualReviewRepository _reviews  = Substitute.For<IManualReviewRepository>();
    private readonly IAuditLogRepository     _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly ApproveReviewCommandHandler _sut;

    public ApproveReviewAuditTests() =>
        _sut = new ApproveReviewCommandHandler(_reviews, _auditLog);

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
    public async Task Handle_Approval_AppendsReviewApprovedAuditEntry()
    {
        var review = MakeCompletedReview();
        _reviews.GetByIdAsync(Arg.Any<ManualReviewId>(), default).Returns(review);

        await _sut.Handle(new ApproveReviewCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        await _auditLog.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.ReviewApproved), default);
    }

    [Fact]
    public async Task Handle_Approval_AuditAfterUpdate()
    {
        var review = MakeCompletedReview();
        _reviews.GetByIdAsync(Arg.Any<ManualReviewId>(), default).Returns(review);

        await _sut.Handle(new ApproveReviewCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        Received.InOrder(() =>
        {
            _reviews.UpdateAsync(review, default);
            _auditLog.AppendAsync(Arg.Any<AuditLog>(), default);
        });
    }
}
