using ExamShield.Domain.Exceptions;
using ExamShield.Infrastructure.Storage;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace ExamShield.UnitTests.Infrastructure.Storage;

public sealed class MinioImageStorageTests
{
    private readonly IObjectStore _objectStore = Substitute.For<IObjectStore>();
    private readonly MinioImageStorage _storage;

    public MinioImageStorageTests() => _storage = new MinioImageStorage(_objectStore);

    [Fact]
    public async Task StoreAsync_ReturnsKeyContainingCaptureId()
    {
        var captureId = Guid.NewGuid();

        var key = await _storage.StoreAsync(captureId, [1, 2, 3]);

        key.Should().Be($"captures/{captureId:N}");
    }

    [Fact]
    public async Task StoreAsync_DelegatesToObjectStore()
    {
        var captureId = Guid.NewGuid();
        byte[] bytes = [1, 2, 3];

        await _storage.StoreAsync(captureId, bytes);

        await _objectStore.Received(1)
            .PutAsync($"captures/{captureId:N}", bytes, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsBytesFromObjectStore()
    {
        const string key = "captures/abc";
        byte[] expected = [9, 8, 7];
        _objectStore.GetAsync(key, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _storage.RetrieveAsync(key);

        result.Should().Equal(expected);
    }

    [Fact]
    public async Task RetrieveAsync_WhenObjectMissing_ThrowsImageNotFoundException()
    {
        const string key = "captures/missing";
        _objectStore.GetAsync(key, Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("not found"));

        var act = () => _storage.RetrieveAsync(key);

        await act.Should().ThrowAsync<ImageNotFoundException>()
            .WithMessage($"*{key}*");
    }
}
