using MediatR;

namespace ExamShield.Application.Queries.ServerVerifyCapture;

public sealed record ServerVerifyCaptureQuery(Guid CaptureId) : IRequest<ServerVerifyResult>;

public sealed record ServerVerifyResult(
    bool IsValid,
    bool HashValid,
    bool SignatureValid,
    string StorageKey,
    string ExpectedHash,
    string ActualHash);
