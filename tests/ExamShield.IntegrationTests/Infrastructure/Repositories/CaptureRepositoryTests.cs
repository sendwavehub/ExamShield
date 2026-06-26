using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;
using ExamShield.Infrastructure.Persistence;
using ExamShield.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ExamShield.IntegrationTests.Infrastructure.Repositories;

public sealed class CaptureRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ExamShieldDbContext _context = null!;
    private CaptureRepository _sut = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ExamShieldDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ExamShieldDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _sut = new CaptureRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static Capture MakeCapture(Hash? hash = null) =>
        Capture.Create(
            ExamId.New(), StudentId.New(), DeviceId.New(),
            new PageNumber(1),
            hash ?? Hash.FromHex(new string('a', 64)),
            new Signature(new byte[64]));

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsPersistedCapture()
    {
        var capture = MakeCapture();

        await _sut.AddAsync(capture);
        var loaded = await _sut.GetByIdAsync(capture.Id);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(capture.Id);
    }

    [Fact]
    public async Task AddAsync_PreservesAllValueObjects()
    {
        var hash = Hash.FromHex(new string('c', 64));
        var capture = MakeCapture(hash);

        await _sut.AddAsync(capture);
        _context.ChangeTracker.Clear(); // force reload from DB

        var loaded = await _sut.GetByIdAsync(capture.Id);

        loaded!.ExamId.Should().Be(capture.ExamId);
        loaded.StudentId.Should().Be(capture.StudentId);
        loaded.DeviceId.Should().Be(capture.DeviceId);
        loaded.PageNumber.Should().Be(capture.PageNumber);
        loaded.ExpectedHash.Should().Be(hash);
        loaded.CapturedAt.Should().BeCloseTo(capture.CapturedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task AddAsync_PreservesSignatureBytes()
    {
        var sigBytes = new byte[] { 0x30, 0x44, 0x02, 0x20 };
        var capture = Capture.Create(
            ExamId.New(), StudentId.New(), DeviceId.New(),
            new PageNumber(1),
            Hash.FromHex(new string('d', 64)),
            new Signature(sigBytes));

        await _sut.AddAsync(capture);
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(capture.Id);

        loaded!.Signature.Bytes.Should().StartWith(sigBytes);
    }

    [Fact]
    public async Task AddAsync_PreservesCreatedStatus()
    {
        var capture = MakeCapture();

        await _sut.AddAsync(capture);
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(capture.Id);

        loaded!.Status.Should().Be(CaptureStatus.Created);
    }

    [Fact]
    public async Task UpdateAsync_PersistsStatusChange()
    {
        var capture = MakeCapture();
        await _sut.AddAsync(capture);
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(capture.Id);
        loaded!.VerifyIntegrity(Hash.FromHex(new string('b', 64))); // mismatch → Tampered
        await _sut.UpdateAsync(loaded);
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(capture.Id);
        reloaded!.Status.Should().Be(CaptureStatus.Tampered);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(CaptureId.New());

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_MultipleCaptures_EachRetrievableById()
    {
        var a = MakeCapture();
        var b = MakeCapture();

        await _sut.AddAsync(a);
        await _sut.AddAsync(b);

        (await _sut.GetByIdAsync(a.Id)).Should().NotBeNull();
        (await _sut.GetByIdAsync(b.Id)).Should().NotBeNull();
    }
}
