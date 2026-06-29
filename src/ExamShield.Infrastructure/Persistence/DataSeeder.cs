using ExamShield.Application.Interfaces;
using ExamShield.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExamShield.Infrastructure.Persistence;

/// <summary>
/// Hosted service that auto-seeds demo data on first startup (development path).
/// Skipped when a SuperAdministrator already exists (wizard was used) or when
/// Features:AutoSeedDemo is false (production default).
/// </summary>
public sealed class DataSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<DataSeeder> logger,
    IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        if (!configuration.GetValue("Features:AutoSeedDemo", defaultValue: true))
        {
            logger.LogInformation("Demo seed disabled via Features:AutoSeedDemo — skipping.");
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ExamShieldDbContext>();

        if (await ctx.Users.AnyAsync(u => u.Role == UserRole.SuperAdministrator, ct))
        {
            logger.LogInformation("SuperAdministrator exists — demo seed skipped (wizard path).");
            return;
        }

        if (await ctx.Exams.AnyAsync(ct))
        {
            logger.LogInformation("Seed data already present — skipping.");
            return;
        }

        logger.LogInformation("Seeding demo data…");
        var seeder = scope.ServiceProvider.GetRequiredService<IDemoDataSeeder>();
        await seeder.SeedAsync(ct);
        logger.LogInformation("Demo seed complete.");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
