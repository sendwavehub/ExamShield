using ExamShield.Application.Queries.GetStudentResults;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Queries;

public sealed class GetStudentResultsPublishedOnlyTests
{
    private readonly ICaptureRepository _captures = Substitute.For<ICaptureRepository>();
    private readonly IScoreRepository _scores = Substitute.For<IScoreRepository>();
    private readonly IExamRepository _exams = Substitute.For<IExamRepository>();
    private readonly GetStudentResultsQueryHandler _sut;

    public GetStudentResultsPublishedOnlyTests() =>
        _sut = new GetStudentResultsQueryHandler(_captures, _scores, _exams);

    private static Score BuildScore(Guid studentId, bool published)
    {
        var score = Score.Create(
            CaptureId.New(), new ExamId(Guid.NewGuid()), new StudentId(studentId),
            new List<ExtractedAnswer> { new(1, "A", new OcrConfidence(1.0)) },
            new AnswerKey(new Dictionary<int, string> { [1] = "A" }));
        if (published) score.Publish();
        return score;
    }

    [Fact]
    public async Task Handle_DoesNotReturnUnpublishedScores()
    {
        var studentId = Guid.NewGuid();
        var unpublishedScore = BuildScore(studentId, published: false);
        var publishedScore   = BuildScore(studentId, published: true);

        _scores.GetPublishedAsync(default)
            .Returns(new List<Score> { publishedScore });
        _captures.ListByStudentIdAsync(Arg.Any<StudentId>(), default)
            .Returns(new List<Capture>());
        _exams.ListAllAsync(default).Returns(new List<Exam>());

        var result = await _sut.Handle(new GetStudentResultsQuery(studentId), default);

        Assert.Single(result.Results);
        Assert.Equal(publishedScore.Id.Value, result.Results[0].ScoreId);
    }

    [Fact]
    public async Task Handle_WhenNoPublishedScores_ReturnsEmpty()
    {
        var studentId = Guid.NewGuid();
        _scores.GetPublishedAsync(default).Returns(new List<Score>());

        var result = await _sut.Handle(new GetStudentResultsQuery(studentId), default);

        Assert.Empty(result.Results);
        await _scores.Received(1).GetPublishedAsync(default);
        await _scores.DidNotReceive().GetAllAsync(default);
    }
}
