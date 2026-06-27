using ExamShield.Api.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExamShield.IntegrationTests;

/// <summary>
/// Injects tight rate limits (3 req / 1-second window) via IPostConfigureOptions
/// so the deferred RateLimiterConfigurator picks them up at container-build time.
/// </summary>
public sealed class RateLimitTestFactory : TestWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IPostConfigureOptions<RateLimitOptions>>(
                new TightRateLimitOptions());
        });
    }

    private sealed class TightRateLimitOptions : IPostConfigureOptions<RateLimitOptions>
    {
        public void PostConfigure(string? name, RateLimitOptions opts)
        {
            opts.Auth   = new RateLimitPolicyOptions { PermitLimit = 3, WindowSeconds = 1 };
            opts.Upload = new RateLimitPolicyOptions { PermitLimit = 3, WindowSeconds = 1 };
        }
    }
}
