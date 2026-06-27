using ExamShield.Application.Queries.ExportScores;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Queries;

public sealed class ExportScoresQueryHandlerTests
{
    private readonly IScoreRepository _repo = Substitute.For<IScoreRepository>();
    private readonly ExportScoresQueryHandler _sut;

    public ExportScoresQueryHandlerTests() => _sut = new ExportScoresQueryHandler(_repo);

    private static Score MakeScore(ExamId examId)
    {
        var captureId  = CaptureId.New();
        var studentId  = StudentId.New();
        var answers    = Array.Empty<ExtractedAnswer>();
        var key        = new AnswerKey(new Dictionary<int, string> { [1] = "A" });
        return Score.Create(captureId, examId, studentId, answers, key);
    }

    [Fact]
    public async Task Handle_WithNoFilter_ExportsAllScores()
    {
        var examId = ExamId.New();
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { MakeScore(examId) });

        var result = await _sut.Handle(new ExportScoresQuery(), default);

        await _repo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        Assert.Contains("ScoreId", result.Csv);
    }

    [Fact]
    public async Task Handle_WithExamIdFilter_UsesGetByExamIdAsync()
    {
        var examId = ExamId.New();
        _repo.GetByExamIdAsync(examId, Arg.Any<CancellationToken>())
            .Returns(new[] { MakeScore(examId) });

        var result = await _sut.Handle(new ExportScoresQuery(ExamId: examId.Value), default);

        await _repo.Received(1).GetByExamIdAsync(examId, Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CsvContainsRequiredColumns()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Score>());

        var result = await _sut.Handle(new ExportScoresQuery(), default);

        Assert.Contains("ScoreId",        result.Csv);
        Assert.Contains("ExamId",         result.Csv);
        Assert.Contains("StudentId",      result.Csv);
        Assert.Contains("CorrectAnswers", result.Csv);
        Assert.Contains("Percentage",     result.Csv);
    }

    [Fact]
    public async Task Handle_WithScores_IncludesDataRows()
    {
        var examId = ExamId.New();
        _repo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { MakeScore(examId), MakeScore(examId) });

        var result = await _sut.Handle(new ExportScoresQuery(), default);

        var lines = result.Csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length); // header + 2 rows
    }

    [Fact]
    public async Task Handle_FilenameStartsWithScores()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Score>());

        var result = await _sut.Handle(new ExportScoresQuery(), default);

        Assert.StartsWith("scores-", result.Filename);
        Assert.EndsWith(".csv", result.Filename);
    }
}
