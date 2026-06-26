using System.Collections.Concurrent;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.IntegrationTests.Fakes;

public sealed class InMemoryDeviceRepository : IDeviceRepository
{
    private readonly ConcurrentDictionary<DeviceId, Device> _store = new();

    public Task AddAsync(Device device, CancellationToken ct = default)
    {
        _store[device.Id] = device;
        return Task.CompletedTask;
    }

    public Task<Device?> GetByIdAsync(DeviceId id, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(id, out var device) ? device : null);
}
