using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace ExamShield.Infrastructure.Storage;

internal sealed class MinioObjectStore(IMinioClient client, string bucketName) : IObjectStore
{
    public async Task PutAsync(string key, byte[] data, CancellationToken ct)
    {
        using var stream = new MemoryStream(data);
        var args = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(key)
            .WithStreamData(stream)
            .WithObjectSize(data.Length)
            .WithContentType("application/octet-stream");
        await client.PutObjectAsync(args, ct);
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
}
