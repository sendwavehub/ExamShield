namespace ExamShield.Api.Contracts;

public sealed record ServerVerifyResponse(
    bool IsValid,
    bool HashValid,
    bool SignatureValid,
    string StorageKey,
    string ExpectedHash,
    string ActualHash);
