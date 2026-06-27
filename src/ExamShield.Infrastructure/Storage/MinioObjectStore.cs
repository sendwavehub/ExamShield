using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.ObjectLock;
using Minio.Exceptions;

namespace ExamShield.Infrastructure.Storage;

public sealed class MinioObjectStore(IMinioClient client, string bucketName, StorageOptions options) : IObjectStore
{
    public async Task PutAsync(string key, byte[] data, CancellationToken ct)
    {
        using var stream = new MemoryStream(data);
        var putArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(key)
            .WithStreamData(stream)
            .WithObjectSize(data.Length)
            .WithContentType("application/octet-stream");

        await client.PutObjectAsync(putArgs, ct);

        if (options.EnableObjectLock)
            await SetRetentionAsync(key, ct);
    }

    public async Task<byte[]> GetAsync(string key, CancellationToken ct)
    {
        var output = new MemoryStream();
        try
        {
            var args = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(key)
                .WithCallbackStream((stream, token) => stream.CopyToAsync(output, token));
            await client.GetObjectAsync(args, ct);
        }
        catch (ObjectNotFoundException)
        {
            throw new KeyNotFoundException($"Object not found: {key}");
        }
        return output.ToArray();
    }

    private async Task SetRetentionAsync(string key, CancellationToken ct)
    {
        var mode = options.RetentionMode == "GOVERNANCE"
            ? ObjectRetentionMode.GOVERNANCE
            : ObjectRetentionMode.COMPLIANCE;

        var retentionArgs = new SetObjectRetentionArgs()
            .WithBucket(bucketName)
            .WithObject(key)
            .WithRetentionMode(mode)
            .WithRetentionUntilDate(DateTime.UtcNow.AddDays(options.RetentionDays));

        await client.SetObjectRetentionAsync(retentionArgs, ct);
    }
}
