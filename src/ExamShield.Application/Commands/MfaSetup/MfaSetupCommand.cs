using MediatR;

namespace ExamShield.Application.Commands.MfaSetup;

public sealed record MfaSetupResult(string Secret, string QrUri);
public sealed record MfaSetupCommand(Guid UserId, string Email) : IRequest<MfaSetupResult>;
