using ExamShield.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ExamShield.Infrastructure.Health;

public sealed class SystemHealthService(HealthCheckService healthChecks) : ISystemHealthService
{
    public async Task<IReadOnlyDictionary<string, string>> CheckAsync(CancellationToken ct = default)
    {
        var report = await healthChecks.CheckHealthAsync(ct);
        var result = report.Entries.ToDictionary(
            e => e.Key,
            e => e.Value.Status.ToString());
        result["api"] = "Healthy";
        return result;
    }
}
