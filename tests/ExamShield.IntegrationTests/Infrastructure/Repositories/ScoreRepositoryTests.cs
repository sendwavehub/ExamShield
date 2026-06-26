using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;
using ExamShield.Infrastructure.Persistence;
using ExamShield.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ExamShield.IntegrationTests.Infrastructure.Repositories;

public sealed class ScoreRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ExamShieldDbContext _context = null!;
    private ScoreRepository _sut = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ExamShieldDbContext>()
            .UseSqlite(_connection).Options;
        _context = new ExamShieldDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _sut = new ScoreRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static readonly AnswerKey Key =
        new(new Dictionary<int, string> { [1] = "A", [2] = "B" });

    private static Score MakeScore() =>
        Score.Create(CaptureId.New(), ExamId.New(), StudentId.New(),
        [
            new ExtractedAnswer(1, "A", new OcrConfidence(0.95)),
            new ExtractedAnswer(2, "B", new OcrConfidence(0.90))
        ], Key);

    [Fact]
    public async Task AddAsync_ThenGetAll_IncludesScore()
    {
        var score = MakeScore();

        await _sut.AddAsync(score);
        var all = await _sut.GetAllAsync();

        all.Should().ContainSingle(s => s.Id == score.Id);
    }

    [Fact]
    public async Task AddAsync_ThenGetAll_PreservesCorrectAnswers()
    {
        var score = MakeScore();

        await _sut.AddAsync(score);
        var all = await _sut.GetAllAsync();

        all[0].CorrectAnswers.Should().Be(2);
        all[0].TotalQuestions.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        var all = await _sut.GetAllAsync();
        all.Should().BeEmpty();
    }
}
