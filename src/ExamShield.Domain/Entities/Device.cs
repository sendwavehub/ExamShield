using ExamShield.Domain.Events;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Entities;

public sealed class Device : AggregateRoot
{
    public DeviceId Id { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public PublicKey PublicKey { get; private set; } = null!;
    public DateTimeOffset RegisteredAt { get; private set; }
    public bool IsActive { get; private set; }

    private Device() { } // EF Core

    public static Device Register(string name, PublicKey publicKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(publicKey, nameof(publicKey));

        var device = new Device
        {
            Id = DeviceId.New(),
            Name = name,
            PublicKey = publicKey,
            RegisteredAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        device.AddDomainEvent(new DeviceRegistered(device.Id));
        return device;
    }
}
