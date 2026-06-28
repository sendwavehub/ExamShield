using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;
using ExamShield.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace ExamShield.UnitTests.Infrastructure.Repositories;

public sealed class InMemoryManualReviewRepositoryTests
{
    private readonly InMemoryManualReviewRepository _sut = new();

    private static ExamShield.Domain.Entities.ManualReview MakeReview()
    {
        var answers = new[] { new ExtractedAnswer(1, "A", new OcrConfidence(0.6)) };
        var ocrResult = ExamShield.Domain.Entities.OcrResult.Create(CaptureId.New(), answers);
        return ExamShield.Domain.Entities.ManualReview.CreateFor(ocrResult);
    }

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsReview()
    {
        var review = MakeReview();
        await _sut.AddAsync(review);

        var found = await _sut.GetByIdAsync(review.Id);
        found.Should().NotBeNull();
        found!.Id.Should().Be(review.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(ManualReviewId.New());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCaptureIdAsync_MatchesByCaptureId()
    {
        var review = MakeReview();
        await _sut.AddAsync(review);

        var found = await _sut.GetByCaptureIdAsync(review.CaptureId);
        found.Should().NotBeNull();
        found!.CaptureId.Should().Be(review.CaptureId);
    }

    [Fact]
    public async Task GetByCaptureIdAsync_WrongCaptureId_ReturnsNull()
    {
        var review = MakeReview();
        await _sut.AddAsync(review);

        var found = await _sut.GetByCaptureIdAsync(CaptureId.New());
        found.Should().BeNull();
    }

    [Fact]
    public async Task GetPendingAsync_OnlyReturnsPendingReviews()
    {
        var pending = MakeReview();
        var completed = MakeReview();
        completed.Complete([new ReviewedAnswer(1, "A")], UserId.New());

        await _sut.AddAsync(pending);
        await _sut.AddAsync(completed);

        var results = await _sut.GetPendingAsync();
        results.Should().ContainSingle().Which.Id.Should().Be(pending.Id);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var review = MakeReview();
        await _sut.AddAsync(review);

        review.Complete([new ReviewedAnswer(1, "B")], UserId.New());
        await _sut.UpdateAsync(review);

        var updated = await _sut.GetByIdAsync(review.Id);
        updated!.Status.Should().Be(ManualReviewStatus.Completed);
    }
}
