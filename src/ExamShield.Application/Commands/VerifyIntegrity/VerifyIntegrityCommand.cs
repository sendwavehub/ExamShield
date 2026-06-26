using MediatR;

namespace ExamShield.Application.Commands.VerifyIntegrity;

public sealed record VerifyIntegrityCommand(
    Guid CaptureId,
    byte[] ImageBytes
) : IRequest<VerifyIntegrityResult>;

public sealed record VerifyIntegrityResult(
    bool IsValid,
    string ExpectedHash,
    string ActualHash
);
