namespace ExamShield.Api.Contracts;

public sealed record PublicVerifyResponse(
    Guid CaptureId,
    bool IsValid,
    bool HashValid,
    bool SignatureValid,
    bool IsUploaded,
    DateTimeOffset? CapturedAt
);
