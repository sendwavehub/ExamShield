using ExamShield.Domain.Exceptions;
using ExamShield.Infrastructure.Storage;
using FluentAssertions;

namespace ExamShield.UnitTests.Infrastructure.Storage;

public sealed class InMemoryImageStorageTests
{
    private readonly InMemoryImageStorage _sut = new();

    [Fact]
    public async Task StoreAsync_ReturnsNonEmptyKey()
    {
        var key = await _sut.StoreAsync(Guid.NewGuid(), new byte[] { 1, 2, 3 });
        key.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task StoreAsync_KeyContainsCaptureId()
    {
        var captureId = Guid.NewGuid();
        var key = await _sut.StoreAsync(captureId, new byte[] { 1 });
        key.Should().Contain(captureId.ToString("N"));
    }

    [Fact]
    public async Task StoreAndRetrieve_ReturnsSameBytes()
    {
        var data = new byte[] { 10, 20, 30, 40 };
        var key = await _sut.StoreAsync(Guid.NewGuid(), data);
        var retrieved = await _sut.RetrieveAsync(key);
        retrieved.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task RetrieveAsync_UnknownKey_ThrowsImageNotFoundException()
    {
        var act = async () => await _sut.RetrieveAsync("captures/does-not-exist");
        await act.Should().ThrowAsync<ImageNotFoundException>();
    }

    [Fact]
    public async Task StoreAsync_OverwritesSameCaptureId()
    {
        var captureId = Guid.NewGuid();
        await _sut.StoreAsync(captureId, new byte[] { 1 });
        var key = await _sut.StoreAsync(captureId, new byte[] { 99 });

        var retrieved = await _sut.RetrieveAsync(key);
        retrieved.Should().BeEquivalentTo(new byte[] { 99 });
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsCopyNotReference()
    {
        var data = new byte[] { 1, 2, 3 };
        var key = await _sut.StoreAsync(Guid.NewGuid(), data);

        var r1 = await _sut.RetrieveAsync(key);
        r1[0] = 99;
        var r2 = await _sut.RetrieveAsync(key);

        r2[0].Should().Be(1); // mutation of r1 should not affect stored copy
    }
}
