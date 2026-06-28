using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ExamShield.Infrastructure.Persistence.Repositories;

public sealed class DeviceCertificateRepository(ExamShieldDbContext db) : IDeviceCertificateRepository
{
    public async Task AddAsync(DeviceCertificate cert, CancellationToken ct = default)
    {
        db.DeviceCertificates.Add(cert);
        await db.SaveChangesAsync(ct);
    }

    public Task<DeviceCertificate?> GetActiveAsync(DeviceId deviceId, CancellationToken ct = default) =>
        db.DeviceCertificates
            .Where(c => c.DeviceId == deviceId && c.RevokedAt == null && c.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(c => c.IssuedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<DeviceCertificate>> GetAllForDeviceAsync(DeviceId deviceId, CancellationToken ct = default) =>
        await db.DeviceCertificates
            .Where(c => c.DeviceId == deviceId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync(ct);

    public async Task UpdateAsync(DeviceCertificate cert, CancellationToken ct = default)
    {
        db.DeviceCertificates.Update(cert);
        await db.SaveChangesAsync(ct);
    }
}
