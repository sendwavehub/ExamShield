namespace ExamShield.Api.Contracts;

public sealed record MfaStatusResponse(bool MfaEnabled);
public sealed record MfaSetupResponse(string Secret, string QrUri);
public sealed record MfaVerifyRequest(string Code);
