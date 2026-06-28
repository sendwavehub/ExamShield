using ExamShield.Application.Commands.ScoreCapture;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ExamShield.UnitTests.Application.Commands.ScoreCapture;

public sealed class ScoreCaptureManualReviewTests
{
    private readonly ICaptureRepository      _captures   = Substitute.For<ICaptureRepository>();
    private readonly IOcrResultRepository    _ocrResults = Substitute.For<IOcrResultRepository>();
    private readonly IAnswerKeyRepository    _answerKeys = Substitute.For<IAnswerKeyRepository>();
    private readonly IScoreRepository        _scores     = Substitute.For<IScoreRepository>();
    private readonly IAuditLogRepository     _auditLog   = Substitute.For<IAuditLogRepository>();
    private readonly ICacheService           _cache      = Substitute.For<ICacheService>();
    private readonly IManualReviewRepository _reviews    = Substitute.For<IManualReviewRepository>();
    private readonly ScoreCaptureCommandHandler _sut;

    public ScoreCaptureManualReviewTests() =>
        _sut = new ScoreCaptureCommandHandler(
            _captures, _ocrResults, _answerKeys, _scores, _auditLog, _cache, _reviews);

    private static Capture UploadedCapture()
    {
        var c = Capture.Create(ExamId.New(), StudentId.New(), DeviceId.New(),
            new PageNumber(1), Hash.FromHex(new string('a', 64)), new Signature(new byte[64]));
        c.RecordUpload("storage/key");
        return c;
    }

    [Fact]
    public async Task Handle_LowConfidenceOcrWithNoReview_ThrowsManualReviewRequiredException()
    {
        var capture = UploadedCapture();
        var lowConfOcr = OcrResult.Create(capture.Id, new[]
        {
            new ExtractedAnswer(1, "A", new OcrConfidence(0.5))  // below threshold
        });

        _captures.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _ocrResults.GetByCaptureIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(lowConfOcr);
        _reviews.GetByCaptureIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns((ManualReview?)null);

        var act = () => _sut.Handle(new ScoreCaptureCommand(capture.Id.Value), default);

        await act.Should().ThrowAsync<ManualReviewRequiredException>();
    }

    [Fact]
    public async Task Handle_LowConfidenceOcrWithCompletedReview_ScoresUsingReviewedAnswers()
    {
        var capture = UploadedCapture();
        var lowConfOcr = OcrResult.Create(capture.Id, new[]
        {
            new ExtractedAnswer(1, "A", new OcrConfidence(0.5))
        });
        var review = ManualReview.CreateFor(lowConfOcr);
        review.Complete(new[] { new ReviewedAnswer(1, "B") }, UserId.New());

        var key = new AnswerKey(new Dictionary<int, string> { [1] = "B" });
        _captures.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _ocrResults.GetByCaptureIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(lowConfOcr);
        _reviews.GetByCaptureIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(review);
        _answerKeys.GetByExamIdAsync(capture.ExamId, Arg.Any<CancellationToken>()).Returns(key);

        var result = await _sut.Handle(new ScoreCaptureCommand(capture.Id.Value), default);

        result.CorrectAnswers.Should().Be(1);
    }

    [Fact]
    public async Task Handle_HighConfidenceOcrWithNoReview_ScoresNormally()
    {
        var capture = UploadedCapture();
        var highConfOcr = OcrResult.Create(capture.Id, new[]
        {
            new ExtractedAnswer(1, "A", new OcrConfidence(0.95))
        });

        var key = new AnswerKey(new Dictionary<int, string> { [1] = "A" });
        _captures.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(capture);
        _ocrResults.GetByCaptureIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns(highConfOcr);
        _reviews.GetByCaptureIdAsync(capture.Id, Arg.Any<CancellationToken>()).Returns((ManualReview?)null);
        _answerKeys.GetByExamIdAsync(capture.ExamId, Arg.Any<CancellationToken>()).Returns(key);

        var result = await _sut.Handle(new ScoreCaptureCommand(capture.Id.Value), default);

        result.CorrectAnswers.Should().Be(1);
    }
}
