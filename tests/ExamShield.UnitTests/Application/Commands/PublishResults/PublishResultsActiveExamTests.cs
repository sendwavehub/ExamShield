using ExamShield.Application.Commands.PublishResults;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.PublishResults;

public sealed class PublishResultsActiveExamTests
{
    private readonly IScoreRepository    _scores   = Substitute.For<IScoreRepository>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly ICacheService       _cache    = Substitute.For<ICacheService>();
    private readonly IExamRepository     _exams    = Substitute.For<IExamRepository>();
    private readonly PublishResultsCommandHandler _sut;

    public PublishResultsActiveExamTests() =>
        _sut = new PublishResultsCommandHandler(_scores, _auditLog, _cache, _exams);

    private static Score MakeUnpublishedScore(ExamId examId)
    {
        var answers = new List<ExtractedAnswer>
        {
            new(1, "A", new OcrConfidence(1.0))
        };
        var key = new AnswerKey(new Dictionary<int, string> { { 1, "A" } });
        return Score.Create(CaptureId.New(), examId, StudentId.New(), answers, key);
    }

    [Fact]
    public async Task Handle_ActiveExam_ThrowsInvalidOperationException()
    {
        var examId = Guid.NewGuid();
        var exam   = Exam.Create("Active Exam", null, 10);
        exam.Activate();

        _exams.GetByIdAsync(Arg.Any<ExamId>(), default).Returns(exam);
        _scores.GetByExamIdAsync(Arg.Any<ExamId>(), default)
               .Returns(new List<Score> { MakeUnpublishedScore(new ExamId(examId)) });

        var act = () => _sut.Handle(new PublishResultsCommand(examId), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*closed*");
    }

    [Fact]
    public async Task Handle_ClosedExam_PublishesSuccessfully()
    {
        var examId = Guid.NewGuid();
        var exam   = Exam.Create("Closed Exam", null, 10);
        exam.Activate();
        exam.Close();

        _exams.GetByIdAsync(Arg.Any<ExamId>(), default).Returns(exam);
        _scores.GetByExamIdAsync(Arg.Any<ExamId>(), default)
               .Returns(new List<Score> { MakeUnpublishedScore(new ExamId(examId)) });

        var result = await _sut.Handle(new PublishResultsCommand(examId), default);

        result.PublishedCount.Should().Be(1);
    }
}
