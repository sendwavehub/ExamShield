using ExamShield.Domain.Entities;

namespace ExamShield.Domain.Interfaces;

public interface ISystemSettingsRepository
{
    Task<SystemSettings> GetAsync(CancellationToken ct = default);
    Task SaveAsync(SystemSettings settings, CancellationToken ct = default);
}
