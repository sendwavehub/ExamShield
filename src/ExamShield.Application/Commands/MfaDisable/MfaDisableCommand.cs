using MediatR;

namespace ExamShield.Application.Commands.MfaDisable;

public sealed record MfaDisableResult(bool MfaEnabled);
public sealed record MfaDisableCommand(Guid UserId) : IRequest<MfaDisableResult>;
