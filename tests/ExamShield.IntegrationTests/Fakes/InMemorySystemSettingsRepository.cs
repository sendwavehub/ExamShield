using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;

namespace ExamShield.IntegrationTests.Fakes;

public sealed class InMemorySystemSettingsRepository : ISystemSettingsRepository
{
    private SystemSettings _settings = SystemSettings.CreateDefault();

    public Task<SystemSettings> GetAsync(CancellationToken ct = default) =>
        Task.FromResult(_settings);

    public Task SaveAsync(SystemSettings settings, CancellationToken ct = default)
    {
        _settings = settings;
        return Task.CompletedTask;
    }
}
