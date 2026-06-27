using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamShield.Infrastructure.Persistence.Repositories;

public sealed class SystemSettingsRepository(ExamShieldDbContext context) : ISystemSettingsRepository
{
    public async Task<SystemSettings> GetAsync(CancellationToken ct = default)
    {
        var settings = await context.SystemSettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = SystemSettings.CreateDefault();
            await context.SystemSettings.AddAsync(settings, ct);
            await context.SaveChangesAsync(ct);
        }
        return settings;
    }

    public async Task SaveAsync(SystemSettings settings, CancellationToken ct = default)
    {
        context.SystemSettings.Update(settings);
        await context.SaveChangesAsync(ct);
    }
}
