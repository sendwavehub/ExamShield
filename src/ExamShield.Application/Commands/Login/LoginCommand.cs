using MediatR;

namespace ExamShield.Application.Commands.Login;

public sealed record LoginCommand(string Email, string Password, string? IpAddress = null) : IRequest<LoginResult>;

public sealed record LoginResult(
    string Token,
    string RefreshToken,
    string Role,
    bool RequiresMfa = false,
    // True when MFA is mandatory for the role but has not been configured yet.
    // The client must redirect to the MFA-setup flow before login can complete.
    bool MfaSetupRequired = false);
