using ExamShield.Application.Commands.ScoreCapture;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.ScoreCapture;

public sealed class ScoreCaptureTamperedTests
{
    private readonly ICaptureRepository      _captures   = Substitute.For<ICaptureRepository>();
    private readonly IOcrResultRepository    _ocrResults = Substitute.For<IOcrResultRepository>();
    private readonly IAnswerKeyRepository    _answerKeys = Substitute.For<IAnswerKeyRepository>();
    private readonly IScoreRepository        _scores     = Substitute.For<IScoreRepository>();
    private readonly IAuditLogRepository     _auditLog   = Substitute.For<IAuditLogRepository>();
    private readonly ICacheService           _cache      = Substitute.For<ICacheService>();
    private readonly IManualReviewRepository _reviews    = Substitute.For<IManualReviewRepository>();

    private readonly ScoreCaptureCommandHandler _sut;

    public ScoreCaptureTamperedTests()
    {
        _sut = new ScoreCaptureCommandHandler(
            _captures, _ocrResults, _answerKeys, _scores, _auditLog, _cache, _reviews);
    }

    private static Capture MakeTamperedCapture()
    {
        var hash    = Hash.FromHex("a" + new string('0', 63));
        var capture = Capture.Create(
            new ExamId(Guid.NewGuid()), new StudentId(Guid.NewGuid()),
            new DeviceId(Guid.NewGuid()), new PageNumber(1), hash, new Signature(new byte[32]));
        capture.RecordUpload("storage-key");
        capture.FlagAsTampered("watermark destroyed");
        return capture;
    }

    [Fact]
    public async Task Handle_TamperedCapture_ThrowsCaptureAlreadyTamperedException()
    {
        var capture = MakeTamperedCapture();
        _captures.GetByIdAsync(Arg.Any<CaptureId>(), default).Returns(capture);
        _scores.ExistsByCaptureIdAsync(capture.Id, default).Returns(false);

        var act = () => _sut.Handle(new ScoreCaptureCommand(capture.Id.Value), default);

        await act.Should().ThrowAsync<CaptureAlreadyTamperedException>();
        await _scores.DidNotReceive().AddAsync(Arg.Any<Score>(), default);
    }
}
