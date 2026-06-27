using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ExamShield.Infrastructure.HealthChecks;

public sealed class RedisHealthCheck(IDistributedCache cache) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var key = "health:ping";
            await cache.SetStringAsync(key, "pong", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            }, ct);
            var result = await cache.GetStringAsync(key, ct);
            return result == "pong"
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Unexpected ping response.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis unavailable.", ex);
        }
    }
}
