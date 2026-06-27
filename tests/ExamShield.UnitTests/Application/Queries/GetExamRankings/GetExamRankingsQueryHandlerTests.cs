using ExamShield.Application.Queries.GetExamRankings;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Queries.GetExamRankings;

public sealed class GetExamRankingsQueryHandlerTests
{
    private readonly IScoreRepository _scores = Substitute.For<IScoreRepository>();

    // Builds a score where `correct` questions match out of an answer key of size `correct + wrong`
    private static Score MakeScore(ExamId examId, StudentId studentId, int correct, int wrong)
    {
        var key     = Enumerable.Range(1, correct + wrong)
                         .ToDictionary(i => i, i => "A");
        var answers = Enumerable.Range(1, correct)
                         .Select(i => new ExtractedAnswer(i, "A", new OcrConfidence(1.0)))
                         .ToList();
        return Score.Create(CaptureId.New(), examId, studentId,
            answers, new AnswerKey(key));
    }

    [Fact]
    public async Task Handle_ReturnsStudentsOrderedByCorrectAnswersDescending()
    {
        var examId   = ExamId.New();
        var studentA = StudentId.New(); // 9/10 = 90%
        var studentB = StudentId.New(); // 7/10 = 70%
        var studentC = StudentId.New(); // 5/10 = 50%

        _scores.GetByExamIdAsync(examId, Arg.Any<CancellationToken>())
            .Returns([
                MakeScore(examId, studentB, 7, 3),
                MakeScore(examId, studentA, 9, 1),
                MakeScore(examId, studentC, 5, 5),
            ]);

        var handler = new GetExamRankingsQueryHandler(_scores);
        var result  = await handler.Handle(new GetExamRankingsQuery(examId.Value), default);

        Assert.Equal(3, result.Rankings.Count);
        Assert.Equal(studentA.Value, result.Rankings[0].StudentId);
        Assert.Equal(studentB.Value, result.Rankings[1].StudentId);
        Assert.Equal(studentC.Value, result.Rankings[2].StudentId);
    }

    [Fact]
    public async Task Handle_AssignsSequentialRankNumbers_WhenNoTies()
    {
        var examId = ExamId.New();
        _scores.GetByExamIdAsync(examId, Arg.Any<CancellationToken>())
            .Returns([
                MakeScore(examId, StudentId.New(), 9, 1),
                MakeScore(examId, StudentId.New(), 7, 3),
                MakeScore(examId, StudentId.New(), 5, 5),
            ]);

        var handler = new GetExamRankingsQueryHandler(_scores);
        var result  = await handler.Handle(new GetExamRankingsQuery(examId.Value), default);

        Assert.Equal(1, result.Rankings[0].Rank);
        Assert.Equal(2, result.Rankings[1].Rank);
        Assert.Equal(3, result.Rankings[2].Rank);
    }

    [Fact]
    public async Task Handle_TiedScores_ShareRankAndNextRankGaps()
    {
        var examId = ExamId.New();
        _scores.GetByExamIdAsync(examId, Arg.Any<CancellationToken>())
            .Returns([
                MakeScore(examId, StudentId.New(), 8, 2), // 80%
                MakeScore(examId, StudentId.New(), 8, 2), // 80% — tie
                MakeScore(examId, StudentId.New(), 5, 5), // 50%
            ]);

        var handler = new GetExamRankingsQueryHandler(_scores);
        var result  = await handler.Handle(new GetExamRankingsQuery(examId.Value), default);

        Assert.Equal(1, result.Rankings[0].Rank); // both 80s are rank 1
        Assert.Equal(1, result.Rankings[1].Rank);
        Assert.Equal(3, result.Rankings[2].Rank); // 50 is rank 3 (gap after two rank-1s)
    }

    [Fact]
    public async Task Handle_EmptyExam_ReturnsEmptyList()
    {
        var examId = ExamId.New();
        _scores.GetByExamIdAsync(examId, Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetExamRankingsQueryHandler(_scores);
        var result  = await handler.Handle(new GetExamRankingsQuery(examId.Value), default);

        Assert.Empty(result.Rankings);
    }

    [Fact]
    public async Task Handle_IncludesPercentageInResult()
    {
        var examId    = ExamId.New();
        var studentId = StudentId.New();
        _scores.GetByExamIdAsync(examId, Arg.Any<CancellationToken>())
            .Returns([MakeScore(examId, studentId, 8, 2)]); // 80%

        var handler = new GetExamRankingsQueryHandler(_scores);
        var result  = await handler.Handle(new GetExamRankingsQuery(examId.Value), default);

        Assert.Equal(80.0, result.Rankings[0].Percentage, precision: 1);
        Assert.Equal(8,    result.Rankings[0].CorrectAnswers);
        Assert.Equal(10,   result.Rankings[0].TotalQuestions);
    }
}
