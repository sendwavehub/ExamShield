using ExamShield.Application.Commands.SubmitReviewRequest;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.SubmitReviewRequest;

public sealed class SubmitReviewRequestDuplicateTests
{
    private readonly ICaptureRepository       _captures = Substitute.For<ICaptureRepository>();
    private readonly IReviewRequestRepository _requests = Substitute.For<IReviewRequestRepository>();
    private readonly IAuditLogRepository      _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly SubmitReviewRequestCommandHandler _sut;

    public SubmitReviewRequestDuplicateTests() =>
        _sut = new SubmitReviewRequestCommandHandler(_captures, _requests, _auditLog);

    private static Capture MakeCapture(StudentId student)
    {
        return Capture.Create(
            new ExamId(Guid.NewGuid()), student, new DeviceId(Guid.NewGuid()),
            new PageNumber(1), Hash.FromHex(new string('a', 64)), new Signature(new byte[64]));
    }

    [Fact]
    public async Task Handle_WhenPendingRequestAlreadyExists_ThrowsDuplicateReviewRequestException()
    {
        var student = new StudentId(Guid.NewGuid());
        var capture = MakeCapture(student);
        _captures.GetByIdAsync(Arg.Any<CaptureId>(), default).Returns(capture);
        _requests.ExistsPendingForCaptureAsync(capture.Id, student, default).Returns(true);

        var cmd = new SubmitReviewRequestCommand(capture.Id.Value, student.Value, "OCR error");
        var act = () => _sut.Handle(cmd, default);

        await act.Should().ThrowAsync<DuplicateReviewRequestException>();
        await _requests.DidNotReceive().AddAsync(Arg.Any<ReviewRequest>(), default);
    }

    [Fact]
    public async Task Handle_WhenNoPendingRequestExists_Succeeds()
    {
        var student = new StudentId(Guid.NewGuid());
        var capture = MakeCapture(student);
        _captures.GetByIdAsync(Arg.Any<CaptureId>(), default).Returns(capture);
        _requests.ExistsPendingForCaptureAsync(capture.Id, student, default).Returns(false);

        var act = () => _sut.Handle(
            new SubmitReviewRequestCommand(capture.Id.Value, student.Value, "OCR error"), default);

        await act.Should().NotThrowAsync();
        await _requests.Received(1).AddAsync(Arg.Any<ReviewRequest>(), default);
    }
}
