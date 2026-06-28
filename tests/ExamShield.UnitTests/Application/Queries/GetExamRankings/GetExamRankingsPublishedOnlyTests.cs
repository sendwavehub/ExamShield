using ExamShield.Application.Queries.GetExamRankings;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ExamShield.UnitTests.Application.Queries.GetExamRankings;

public sealed class GetExamRankingsPublishedOnlyTests
{
    private readonly IScoreRepository _scores = Substitute.For<IScoreRepository>();
    private readonly GetExamRankingsQueryHandler _sut;

    public GetExamRankingsPublishedOnlyTests() => _sut = new GetExamRankingsQueryHandler(_scores);

    private static Score MakeScore(ExamId examId, bool published)
    {
        var answers = new List<ExtractedAnswer> { new(1, "A", new OcrConfidence(1.0)) };
        var key     = new AnswerKey(new Dictionary<int, string> { [1] = "A" });
        var score   = Score.Create(CaptureId.New(), examId, StudentId.New(), answers, key);
        if (published) score.Publish();
        return score;
    }

    [Fact]
    public async Task Handle_UnpublishedScore_IsExcludedFromRankings()
    {
        var examId = ExamId.New();
        _scores.GetByExamIdAsync(examId, Arg.Any<CancellationToken>())
               .Returns(new List<Score> { MakeScore(examId, published: false) });

        var result = await _sut.Handle(new GetExamRankingsQuery(examId.Value), default);

        result.Rankings.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MixedPublishedAndUnpublished_OnlyPublishedAppear()
    {
        var examId = ExamId.New();
        _scores.GetByExamIdAsync(examId, Arg.Any<CancellationToken>())
               .Returns(new List<Score>
               {
                   MakeScore(examId, published: true),
                   MakeScore(examId, published: false),
               });

        var result = await _sut.Handle(new GetExamRankingsQuery(examId.Value), default);

        result.Rankings.Should().HaveCount(1);
    }
}
