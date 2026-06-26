using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Events;

public sealed record DeviceRegistered(DeviceId DeviceId) : DomainEvent;
