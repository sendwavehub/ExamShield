using ExamShield.Application.Queries.GetScoreBreakdown;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Queries;

public sealed class GetScoreBreakdownTests
{
    private readonly IScoreRepository _scores = Substitute.For<IScoreRepository>();
    private readonly ICaptureRepository _captures = Substitute.For<ICaptureRepository>();
    private readonly IOcrResultRepository _ocrResults = Substitute.For<IOcrResultRepository>();
    private readonly IManualReviewRepository _reviews = Substitute.For<IManualReviewRepository>();
    private readonly IAnswerKeyRepository _answerKeys = Substitute.For<IAnswerKeyRepository>();
    private readonly GetScoreBreakdownQueryHandler _sut;

    private static readonly CaptureId CaptureId = CaptureId.New();
    private static readonly ExamId ExamId = new(Guid.NewGuid());
    private static readonly StudentId StudentId = new(Guid.NewGuid());

    public GetScoreBreakdownTests() =>
        _sut = new GetScoreBreakdownQueryHandler(_scores, _captures, _ocrResults, _reviews, _answerKeys);

    private static Capture MakeCapture()
    {
        var hash = Hash.FromHex("a" + new string('0', 63));
        var sig  = new Signature(new byte[32]);
        return Capture.Create(ExamId, StudentId,
            new DeviceId(Guid.NewGuid()), new PageNumber(1), hash, sig);
    }

    private static OcrResult MakeOcrResult(CaptureId captureId, params (int q, string a)[] answers)
    {
        var extracted = answers
            .Select(t => new ExtractedAnswer(t.q, t.a, new OcrConfidence(0.95)))
            .ToList<ExtractedAnswer>();
        return OcrResult.Create(captureId, extracted, 0.8);
    }

    private static ExamAnswerKey MakeKey(params (int q, string a)[] answers)
    {
        var dict = answers.ToDictionary(t => t.q, t => t.a);
        return ExamAnswerKey.Create(ExamId, dict);
    }

    [Fact]
    public async Task Handle_ReturnsPerQuestionBreakdown()
    {
        var capture = MakeCapture();
        var answerKey = MakeKey((1, "A"), (2, "B"), (3, "C"));
        var ocrResult = MakeOcrResult(CaptureId, (1, "A"), (2, "X"), (3, "C"));

        _scores.GetByCaptureIdAsync(CaptureId, default)
            .Returns(Score.Create(CaptureId, ExamId, StudentId,
                new List<ExtractedAnswer>
                {
                    new(1, "A", new OcrConfidence(0.95)),
                    new(2, "X", new OcrConfidence(0.95)),
                    new(3, "C", new OcrConfidence(0.95)),
                }, new AnswerKey(new Dictionary<int, string> { [1] = "A", [2] = "B", [3] = "C" })));
        _captures.GetByIdAsync(CaptureId, default).Returns(capture);
        _ocrResults.GetByCaptureIdAsync(CaptureId, default).Returns(ocrResult);
        _reviews.GetByCaptureIdAsync(CaptureId, default).Returns((ManualReview?)null);
        _answerKeys.GetEntityByExamIdAsync(ExamId, default).Returns(answerKey);

        var result = await _sut.Handle(new GetScoreBreakdownQuery(CaptureId.Value), default);

        Assert.Equal(3, result.Questions.Count);
        Assert.True(result.Questions[0].IsCorrect);   // Q1: A == A
        Assert.False(result.Questions[1].IsCorrect);  // Q2: X != B
        Assert.True(result.Questions[2].IsCorrect);   // Q3: C == C
    }

    [Fact]
    public async Task Handle_WhenReviewedAnswers_UsesThemOverOcr()
    {
        var capture = MakeCapture();
        var answerKey = MakeKey((1, "A"), (2, "B"));
        var ocrResult = MakeOcrResult(CaptureId, (1, "A"), (2, "X"));

        var review = ManualReview.CreateFor(
            OcrResult.Create(CaptureId,
                new List<ExtractedAnswer> { new(1, "A", new OcrConfidence(0.5)) }, 0.8));
        review.Complete(
            new List<ReviewedAnswer> { new(1, "A"), new(2, "B") },
            new UserId(Guid.NewGuid()));

        _scores.GetByCaptureIdAsync(CaptureId, default)
            .Returns(Score.Create(CaptureId, ExamId, StudentId,
                new List<ExtractedAnswer>
                {
                    new(1, "A", new OcrConfidence(0.95)),
                    new(2, "B", new OcrConfidence(1.0)),
                }, new AnswerKey(new Dictionary<int, string> { [1] = "A", [2] = "B" })));
        _captures.GetByIdAsync(CaptureId, default).Returns(capture);
        _ocrResults.GetByCaptureIdAsync(CaptureId, default).Returns(ocrResult);
        _reviews.GetByCaptureIdAsync(CaptureId, default).Returns(review);
        _answerKeys.GetEntityByExamIdAsync(ExamId, default).Returns(answerKey);

        var result = await _sut.Handle(new GetScoreBreakdownQuery(CaptureId.Value), default);

        Assert.All(result.Questions, q => Assert.True(q.IsCorrect));
    }

    [Fact]
    public async Task Handle_ScoreNotFound_ThrowsKeyNotFoundException()
    {
        _scores.GetByCaptureIdAsync(CaptureId, default).Returns((Score?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.Handle(new GetScoreBreakdownQuery(CaptureId.Value), default));
    }
}
