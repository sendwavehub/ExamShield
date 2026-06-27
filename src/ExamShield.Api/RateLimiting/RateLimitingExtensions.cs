using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ExamShield.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public const string AuthPolicy   = "auth";
    public const string UploadPolicy = "upload";
    public const string GlobalPolicy = "global";

    public static IServiceCollection AddExamShieldRateLimiting(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Bind our typed options — deferred so ConfigureServices overrides take effect.
        services.Configure<RateLimitOptions>(configuration.GetSection("RateLimiting"));

        // Register the configurator that reads IOptions<RateLimitOptions> at container-build time.
        services.AddSingleton<IConfigureOptions<RateLimiterOptions>, RateLimiterConfigurator>();

        services.AddRateLimiter(rl =>
        {
            rl.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            rl.OnRejected = static async (ctx, ct) =>
            {
                ctx.HttpContext.Response.Headers.RetryAfter =
                    ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? ((int)retryAfter.TotalSeconds).ToString()
                        : "60";
                ctx.HttpContext.Response.ContentType = "application/json";
                await ctx.HttpContext.Response.WriteAsync(
                    "{\"error\":\"Rate limit exceeded. Please retry later.\"}", ct);
            };
        });

        return services;
    }
}

/// <summary>
/// Configures the rate limiter policies after all ConfigureServices hooks have run,
/// so test factories can override <see cref="RateLimitOptions"/> via PostConfigure.
/// </summary>
internal sealed class RateLimiterConfigurator(IOptions<RateLimitOptions> opts)
    : IConfigureOptions<RateLimiterOptions>
{
    public void Configure(RateLimiterOptions rl)
    {
        var o = opts.Value;

        rl.AddFixedWindowLimiter(RateLimitingExtensions.AuthPolicy, limiter =>
        {
            limiter.PermitLimit = o.Auth.PermitLimit;
            limiter.Window      = TimeSpan.FromSeconds(o.Auth.WindowSeconds);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit  = 0;
        });

        rl.AddFixedWindowLimiter(RateLimitingExtensions.UploadPolicy, limiter =>
        {
            limiter.PermitLimit = o.Upload.PermitLimit;
            limiter.Window      = TimeSpan.FromSeconds(o.Upload.WindowSeconds);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit  = 0;
        });

        rl.AddFixedWindowLimiter(RateLimitingExtensions.GlobalPolicy, limiter =>
        {
            limiter.PermitLimit = o.Global.PermitLimit;
            limiter.Window      = TimeSpan.FromSeconds(o.Global.WindowSeconds);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit  = 0;
        });
    }
}
