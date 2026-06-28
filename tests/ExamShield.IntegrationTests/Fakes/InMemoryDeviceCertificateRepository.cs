using System.Collections.Concurrent;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.IntegrationTests.Fakes;

public sealed class InMemoryDeviceCertificateRepository : IDeviceCertificateRepository
{
    private readonly ConcurrentDictionary<Guid, DeviceCertificate> _store = new();

    public Task AddAsync(DeviceCertificate cert, CancellationToken ct = default)
    {
        _store[cert.Id] = cert;
        return Task.CompletedTask;
    }

    public Task<DeviceCertificate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public Task<DeviceCertificate?> GetActiveAsync(DeviceId deviceId, CancellationToken ct = default)
    {
        var result = _store.Values
            .Where(c => c.DeviceId == deviceId && c.IsValid)
            .OrderByDescending(c => c.IssuedAt)
            .FirstOrDefault();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<DeviceCertificate>> GetAllForDeviceAsync(DeviceId deviceId, CancellationToken ct = default)
    {
        IReadOnlyList<DeviceCertificate> result = _store.Values
            .Where(c => c.DeviceId == deviceId)
            .OrderByDescending(c => c.IssuedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task UpdateAsync(DeviceCertificate cert, CancellationToken ct = default)
    {
        _store[cert.Id] = cert;
        return Task.CompletedTask;
    }
}
