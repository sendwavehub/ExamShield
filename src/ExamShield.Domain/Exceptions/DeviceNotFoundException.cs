namespace ExamShield.Domain.Exceptions;

public sealed class DeviceNotFoundException : Exception
{
    public DeviceNotFoundException(Guid deviceId)
        : base($"Device {deviceId} was not found.")
    { }
}
