using ExamShield.Infrastructure.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;
using Minio.DataModel.Args;

namespace ExamShield.Infrastructure.HealthChecks;

public sealed class MinioHealthCheck(IMinioClient client, StorageOptions options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var args = new BucketExistsArgs().WithBucket(options.BucketName);
            await client.BucketExistsAsync(args, ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MinIO unavailable.", ex);
        }
    }
}
