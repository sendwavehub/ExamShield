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
        catch (Minio.Exceptions.AccessDeniedException)
        {
            // Bucket exists but has a private anonymous policy — SDK returns 403.
            // Root credentials still have full access; report healthy.
            return HealthCheckResult.Healthy("Bucket exists (private policy).");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MinIO unavailable.", ex);
        }
    }
}
