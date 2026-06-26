namespace ExamShield.Api.Contracts;

public sealed record RegisterDeviceRequest(string Name, byte[] PublicKeyBytes);

public sealed record RegisterDeviceResponse(Guid DeviceId);
