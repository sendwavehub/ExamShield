using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;
using ExamShield.Infrastructure.Persistence;
using ExamShield.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ExamShield.IntegrationTests.Infrastructure.Repositories;

public sealed class OcrResultRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ExamShieldDbContext _context = null!;
    private OcrResultRepository _sut = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ExamShieldDbContext>()
            .UseSqlite(_connection).Options;
        _context = new ExamShieldDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _sut = new OcrResultRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static OcrResult MakeHighConfidenceResult(CaptureId? captureId = null) =>
        OcrResult.Create(captureId ?? CaptureId.New(),
        [
            new ExtractedAnswer(1, "A", new OcrConfidence(0.95)),
            new ExtractedAnswer(2, "B", new OcrConfidence(0.90))
        ]);

    [Fact]
    public async Task AddAsync_ThenGetByCaptureId_ReturnsOcrResult()
    {
        var captureId = CaptureId.New();
        var result = MakeHighConfidenceResult(captureId);

        await _sut.AddAsync(result);
        var loaded = await _sut.GetByCaptureIdAsync(captureId);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(result.Id);
    }

    [Fact]
    public async Task AddAsync_ThenGetByCaptureId_PreservesAnswerCount()
    {
        var captureId = CaptureId.New();
        var result = MakeHighConfidenceResult(captureId);

        await _sut.AddAsync(result);
        var loaded = await _sut.GetByCaptureIdAsync(captureId);

        loaded!.Answers.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddAsync_ThenGetByCaptureId_PreservesAnswerText()
    {
        var captureId = CaptureId.New();
        var result = MakeHighConfidenceResult(captureId);

        await _sut.AddAsync(result);
        var loaded = await _sut.GetByCaptureIdAsync(captureId);

        loaded!.Answers[0].Text.Should().Be("A");
        loaded.Answers[1].Text.Should().Be("B");
    }

    [Fact]
    public async Task AddAsync_ThenGetByCaptureId_PreservesStatus()
    {
        var captureId = CaptureId.New();
        var result = MakeHighConfidenceResult(captureId);

        await _sut.AddAsync(result);
        var loaded = await _sut.GetByCaptureIdAsync(captureId);

        loaded!.Status.Should().Be(result.Status);
    }

    [Fact]
    public async Task GetByCaptureIdAsync_WhenNotFound_ReturnsNull()
    {
        var loaded = await _sut.GetByCaptureIdAsync(CaptureId.New());
        loaded.Should().BeNull();
    }
}
