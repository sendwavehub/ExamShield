using MediatR;

namespace ExamShield.Application.Commands.MfaVerify;

public sealed record MfaVerifyResult(bool MfaEnabled);
public sealed record MfaVerifyCommand(Guid UserId, string Code) : IRequest<MfaVerifyResult>;
