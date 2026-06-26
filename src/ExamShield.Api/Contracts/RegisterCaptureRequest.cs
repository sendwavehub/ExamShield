namespace ExamShield.Api.Contracts;

public sealed record RegisterCaptureRequest(
    Guid ExamId,
    Guid StudentId,
    Guid DeviceId,
    int PageNumber,
    string HashHex,
    byte[] SignatureBytes
);

public sealed record RegisterCaptureResponse(Guid CaptureId);
