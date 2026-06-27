namespace ExamShield.Api.Contracts;

public sealed record RegisterDeviceRequest(string Name, byte[] PublicKeyBytes);

public sealed record RegisterDeviceResponse(Guid DeviceId);

public sealed record DeviceResponse(Guid DeviceId, string Name, bool IsActive, DateTimeOffset RegisteredAt);

public sealed record DeviceListResponse(IReadOnlyList<DeviceResponse> Devices);
