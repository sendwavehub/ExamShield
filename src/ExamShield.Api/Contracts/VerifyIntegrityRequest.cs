namespace ExamShield.Api.Contracts;

public sealed record VerifyIntegrityRequest(byte[] ImageBytes);

public sealed record VerifyIntegrityResponse(bool IsValid, string ExpectedHash, string ActualHash);
