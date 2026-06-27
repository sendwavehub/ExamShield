using ExamShield.Application.Queries.GetExamRankings;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Queries;

public sealed class RankingTieBreakTests
{
    private readonly IScoreRepository _scores = Substitute.For<IScoreRepository>();
    private readonly GetExamRankingsQueryHandler _sut;
    private static readonly ExamId ExamId = new(Guid.NewGuid());

    public RankingTieBreakTests() => _sut = new GetExamRankingsQueryHandler(_scores);

    private const int Total = 10;

    private static Score BuildScore(int correct, DateTimeOffset scoredAt)
    {
        var captureId = CaptureId.New();
        var studentId = new StudentId(Guid.NewGuid());
        // First `correct` questions answered correctly; remaining left blank (wrong)
        var answers = Enumerable.Range(1, correct)
            .Select(q => new ExtractedAnswer(q, "A", new OcrConfidence(1.0)))
            .ToList<ExtractedAnswer>();
        var key = new AnswerKey(Enumerable.Range(1, Total).ToDictionary(q => q, _ => "A"));
        return Score.Create(captureId, ExamId, studentId, answers, key, scoredAt);
    }

    [Fact]
    public async Task TiedStudents_AlwaysReturnedInScoredAtOrder()
    {
        var early = BuildScore(8, DateTimeOffset.UtcNow.AddMinutes(-10));
        var late  = BuildScore(8, DateTimeOffset.UtcNow);
        _scores.GetByExamIdAsync(ExamId, default).Returns(new List<Score> { late, early });

        var result = await _sut.Handle(new GetExamRankingsQuery(ExamId.Value), default);

        Assert.Equal(2, result.Rankings.Count);
        Assert.Equal(early.StudentId.Value, result.Rankings[0].StudentId);
        Assert.Equal(late.StudentId.Value,  result.Rankings[1].StudentId);
        Assert.Equal(1, result.Rankings[0].Rank);
        Assert.Equal(1, result.Rankings[1].Rank);
    }

    [Fact]
    public async Task TiedStudents_SameRankAssigned()
    {
        var a = BuildScore(7, DateTimeOffset.UtcNow.AddMinutes(-5));
        var b = BuildScore(7, DateTimeOffset.UtcNow);
        var c = BuildScore(5, DateTimeOffset.UtcNow);
        _scores.GetByExamIdAsync(ExamId, default).Returns(new List<Score> { b, c, a });

        var result = await _sut.Handle(new GetExamRankingsQuery(ExamId.Value), default);

        Assert.Equal(1, result.Rankings[0].Rank);
        Assert.Equal(1, result.Rankings[1].Rank);
        Assert.Equal(3, result.Rankings[2].Rank);
    }

    [Fact]
    public async Task NoTie_OrderedByPercentageDescending()
    {
        var high = BuildScore(9, DateTimeOffset.UtcNow);
        var low  = BuildScore(5, DateTimeOffset.UtcNow.AddMinutes(-5));
        _scores.GetByExamIdAsync(ExamId, default).Returns(new List<Score> { low, high });

        var result = await _sut.Handle(new GetExamRankingsQuery(ExamId.Value), default);

        Assert.Equal(high.StudentId.Value, result.Rankings[0].StudentId);
        Assert.Equal(1, result.Rankings[0].Rank);
        Assert.Equal(2, result.Rankings[1].Rank);
    }
}
