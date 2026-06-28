using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;
using ExamShield.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace ExamShield.UnitTests.Infrastructure.Repositories;

public sealed class InMemoryScoreRepositoryTests
{
    private readonly InMemoryScoreRepository _sut = new();

    private static Score MakeScore(ExamId? examId = null, CaptureId? captureId = null)
    {
        var eid = examId ?? ExamId.New();
        var cid = captureId ?? CaptureId.New();
        var key = new AnswerKey(new Dictionary<int, string> { [1] = "A" });
        var answers = new[] { new ExtractedAnswer(1, "A", new OcrConfidence(0.9)) };
        return Score.Create(cid, eid, StudentId.New(), answers, key);
    }

    [Fact]
    public async Task AddAsync_ThenGetAll_ContainsScore()
    {
        var score = MakeScore();
        await _sut.AddAsync(score);

        var all = await _sut.GetAllAsync();
        all.Should().ContainSingle().Which.Id.Should().Be(score.Id);
    }

    [Fact]
    public async Task GetPublishedAsync_OnlyReturnsPublished()
    {
        var unpublished = MakeScore();
        var published = MakeScore();
        published.Publish();

        await _sut.AddAsync(unpublished);
        await _sut.AddAsync(published);

        var results = await _sut.GetPublishedAsync();
        results.Should().ContainSingle().Which.Id.Should().Be(published.Id);
    }

    [Fact]
    public async Task GetByExamIdAsync_FiltersCorrectly()
    {
        var targetExamId = ExamId.New();
        var match = MakeScore(targetExamId);
        var other = MakeScore();

        await _sut.AddAsync(match);
        await _sut.AddAsync(other);

        var results = await _sut.GetByExamIdAsync(targetExamId);
        results.Should().ContainSingle().Which.ExamId.Should().Be(targetExamId);
    }

    [Fact]
    public async Task ExistsByCaptureIdAsync_ExistingCapture_ReturnsTrue()
    {
        var captureId = CaptureId.New();
        await _sut.AddAsync(MakeScore(captureId: captureId));

        var exists = await _sut.ExistsByCaptureIdAsync(captureId);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByCaptureIdAsync_MissingCapture_ReturnsFalse()
    {
        await _sut.AddAsync(MakeScore());
        var exists = await _sut.ExistsByCaptureIdAsync(CaptureId.New());
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetByCaptureIdAsync_ReturnsCorrectScore()
    {
        var captureId = CaptureId.New();
        var score = MakeScore(captureId: captureId);
        await _sut.AddAsync(score);

        var found = await _sut.GetByCaptureIdAsync(captureId);
        found.Should().NotBeNull();
        found!.CaptureId.Should().Be(captureId);
    }

    [Fact]
    public async Task UpdateAsync_PersistsPublishedState()
    {
        var score = MakeScore();
        await _sut.AddAsync(score);
        score.Publish();
        await _sut.UpdateAsync(score);

        var results = await _sut.GetPublishedAsync();
        results.Should().ContainSingle().Which.IsPublished.Should().BeTrue();
    }
}
