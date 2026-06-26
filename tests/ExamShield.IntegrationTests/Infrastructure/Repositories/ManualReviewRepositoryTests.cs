using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;
using ExamShield.Infrastructure.Persistence;
using ExamShield.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ExamShield.IntegrationTests.Infrastructure.Repositories;

public sealed class ManualReviewRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ExamShieldDbContext _context = null!;
    private ManualReviewRepository _sut = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ExamShieldDbContext>()
            .UseSqlite(_connection).Options;
        _context = new ExamShieldDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _sut = new ManualReviewRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static ManualReview MakePendingReview()
    {
        var ocr = OcrResult.Create(CaptureId.New(),
            [new ExtractedAnswer(1, "A", new OcrConfidence(0.50))]);
        return ManualReview.CreateFor(ocr);
    }

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsPendingReview()
    {
        var review = MakePendingReview();

        await _sut.AddAsync(review);
        var loaded = await _sut.GetByIdAsync(review.Id);

        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(ManualReviewStatus.Pending);
    }

    [Fact]
    public async Task GetPendingAsync_ReturnsPendingReviews()
    {
        var review = MakePendingReview();
        await _sut.AddAsync(review);

        var pending = await _sut.GetPendingAsync();

        pending.Should().ContainSingle(r => r.Id == review.Id);
    }

    [Fact]
    public async Task GetPendingAsync_ExcludesCompletedReviews()
    {
        var review = MakePendingReview();
        await _sut.AddAsync(review);
        review.Complete([new ReviewedAnswer(1, "B")], UserId.New());
        await _sut.UpdateAsync(review);

        var pending = await _sut.GetPendingAsync();

        pending.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_AfterComplete_PersistsCompletedStatus()
    {
        var review = MakePendingReview();
        await _sut.AddAsync(review);

        review.Complete([new ReviewedAnswer(1, "B")], UserId.New());
        await _sut.UpdateAsync(review);

        // Use a fresh context to confirm persistence
        var freshContext = new ExamShieldDbContext(
            new DbContextOptionsBuilder<ExamShieldDbContext>().UseSqlite(_connection).Options);
        var freshSut = new ManualReviewRepository(freshContext);
        var loaded = await freshSut.GetByIdAsync(review.Id);

        loaded!.Status.Should().Be(ManualReviewStatus.Completed);
        await freshContext.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(ManualReviewId.New());
        loaded.Should().BeNull();
    }
}
