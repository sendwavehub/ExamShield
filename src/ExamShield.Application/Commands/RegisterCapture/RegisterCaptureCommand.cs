using MediatR;

namespace ExamShield.Application.Commands.RegisterCapture;

public sealed record RegisterCaptureCommand(
    Guid ExamId,
    Guid StudentId,
    Guid DeviceId,
    int PageNumber,
    string HashHex,
    byte[] SignatureBytes
) : IRequest<RegisterCaptureResult>;

public sealed record RegisterCaptureResult(Guid CaptureId);
