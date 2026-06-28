using ExamShield.Domain.Enums;
using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;
using ExamShield.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace ExamShield.UnitTests.Infrastructure.Repositories;

public sealed class InMemoryOcrResultRepositoryTests
{
    private readonly InMemoryOcrResultRepository _sut = new();

    private static OcrResult MakeResult(CaptureId? captureId = null, double confidence = 0.95)
    {
        var answers = new[] { new ExtractedAnswer(1, "A", new OcrConfidence(confidence)) };
        return OcrResult.Create(captureId ?? CaptureId.New(), answers);
    }

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsResult()
    {
        var result = MakeResult();
        await _sut.AddAsync(result);

        var found = await _sut.GetByIdAsync(result.Id);
        found.Should().NotBeNull();
        found!.Id.Should().Be(result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var found = await _sut.GetByIdAsync(OcrResultId.New());
        found.Should().BeNull();
    }

    [Fact]
    public async Task GetByCaptureIdAsync_MatchesByCaptureId()
    {
        var captureId = CaptureId.New();
        var result = MakeResult(captureId);
        await _sut.AddAsync(result);

        var found = await _sut.GetByCaptureIdAsync(captureId);
        found.Should().NotBeNull();
        found!.CaptureId.Should().Be(captureId);
    }

    [Fact]
    public async Task GetByCaptureIdAsync_WrongCaptureId_ReturnsNull()
    {
        await _sut.AddAsync(MakeResult());
        var found = await _sut.GetByCaptureIdAsync(CaptureId.New());
        found.Should().BeNull();
    }

    [Fact]
    public async Task ListCompletedAsync_ReturnsOnlyCompleted()
    {
        var lowConfidence = MakeResult(confidence: 0.3); // OcrStatus.LowConfidence
        var completed = MakeResult(confidence: 0.95);    // OcrStatus.Completed

        await _sut.AddAsync(lowConfidence);
        await _sut.AddAsync(completed);

        var results = await _sut.ListCompletedAsync();
        results.Should().ContainSingle().Which.Id.Should().Be(completed.Id);
    }

    [Fact]
    public async Task ListByCaptureIdsAsync_FiltersCorrectly()
    {
        var id1 = CaptureId.New();
        var id2 = CaptureId.New();
        var r1 = MakeResult(id1);
        var r2 = MakeResult(id2);
        var r3 = MakeResult(); // not in list

        await _sut.AddAsync(r1);
        await _sut.AddAsync(r2);
        await _sut.AddAsync(r3);

        var results = await _sut.ListByCaptureIdsAsync([id1, id2]);
        results.Should().HaveCount(2);
        results.Select(r => r.CaptureId).Should().BeEquivalentTo(new[] { id1, id2 });
    }
}
