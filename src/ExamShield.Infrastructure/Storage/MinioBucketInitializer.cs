using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace ExamShield.Infrastructure.Storage;

public sealed class MinioBucketInitializer(
    IMinioClient client,
    StorageOptions options,
    ILogger<MinioBucketInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var existsArgs = new BucketExistsArgs().WithBucket(options.BucketName);
        var exists = await client.BucketExistsAsync(existsArgs, ct);
        if (exists) return;

        var makeArgs = new MakeBucketArgs().WithBucket(options.BucketName);
        if (options.EnableObjectLock)
            makeArgs = makeArgs.WithObjectLock();

        await client.MakeBucketAsync(makeArgs, ct);
        logger.LogInformation(
            "Created MinIO bucket '{Bucket}' (ObjectLock={Lock})",
            options.BucketName, options.EnableObjectLock);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
