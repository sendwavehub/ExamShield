using ExamShield.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Minio;
using Minio.DataModel.Args;
using NSubstitute;
using Xunit;

namespace ExamShield.UnitTests.Infrastructure.Storage;

public sealed class MinioBucketInitializerTests
{
    private readonly IMinioClient _client = Substitute.For<IMinioClient>();
    private readonly StorageOptions _options = new()
    {
        BucketName = "test-bucket",
        EnableObjectLock = true
    };

    private MinioBucketInitializer BuildSut() =>
        new(_client, _options, NullLogger<MinioBucketInitializer>.Instance);

    [Fact]
    public async Task StartAsync_WhenBucketDoesNotExist_CreatesBucket()
    {
        _client.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await BuildSut().StartAsync(default);

        await _client.Received(1).MakeBucketAsync(
            Arg.Any<MakeBucketArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WhenBucketExists_SkipsCreation()
    {
        _client.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await BuildSut().StartAsync(default);

        await _client.DidNotReceive().MakeBucketAsync(
            Arg.Any<MakeBucketArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_AlwaysChecksBucketExistence()
    {
        _client.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await BuildSut().StartAsync(default);

        await _client.Received(1).BucketExistsAsync(
            Arg.Any<BucketExistsArgs>(),
            Arg.Any<CancellationToken>());
    }
}
