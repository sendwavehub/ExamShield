using System.Security.Cryptography;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ISecurityEventRepository _security;
    private readonly IAuditLogRepository _auditLog;
    private readonly LoginOptions _options;
    private readonly IAlertService _alerts;

    public LoginCommandHandler(
        IUserRepository users, IPasswordHasher hasher,
        IJwtTokenService jwt, IRefreshTokenRepository refreshTokens,
        ISecurityEventRepository security, IAuditLogRepository auditLog,
        LoginOptions? options = null, IAlertService? alertService = null)
    {
        _users = users; _hasher = hasher; _jwt = jwt;
        _refreshTokens = refreshTokens; _security = security; _auditLog = auditLog;
        _options = options ?? new LoginOptions();
        _alerts = alertService ?? NullAlertService.Instance;
    }

    public async Task<LoginResult> Handle(LoginCommand command, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(new Email(command.Email), ct);

        if (user is null || !user.IsActive || user.IsLockedOut ||
            !_hasher.Verify(command.Password, user.PasswordHash))
        {
            if (user is not null && user.IsActive && !user.IsLockedOut)
            {
                user.RecordFailedLogin(MaxFailedAttempts, LockoutDuration);
                await _users.SaveAsync(user, ct);

                if (user.IsLockedOut)
                    await _alerts.SendAsync(AlertType.SuspiciousLogin,
                        $"Account locked after repeated failed logins: {command.Email}", ct);
            }

            await _security.AddAsync(SecurityEvent.Create(
                SecurityEventType.LoginFailed, SecuritySeverity.Warning,
                $"Failed login attempt for {command.Email}",
                ipAddress: command.IpAddress), ct);
            throw new InvalidCredentialsException();
        }

        user.ResetFailedLogin();
        await _users.SaveAsync(user, ct);

        // Privileged roles must complete MFA — redirect to MFA step whether enabled or not.
        // If MFA is not yet configured for a privileged role, login is blocked until setup.
        if (user.MfaEnabled)
            return new LoginResult(string.Empty, string.Empty, user.Role.ToString(), RequiresMfa: true);

        if (_options.EnforceMfaForPrivilegedRoles && RequiresMfa(user.Role))
            return new LoginResult(string.Empty, string.Empty, user.Role.ToString(), MfaSetupRequired: true);

        var rawToken = GenerateRawToken();
        var hash = HashToken(rawToken);
        var refreshToken = RefreshToken.Create(user.Id, hash, expiryDays: 7);
        await _refreshTokens.AddAsync(refreshToken, ct);

        await _security.AddAsync(SecurityEvent.Create(
            SecurityEventType.LoginSuccess, SecuritySeverity.Info,
            $"Successful login for {command.Email}",
            userId: user.Id.Value.ToString(),
            ipAddress: command.IpAddress), ct);

        await _auditLog.AppendAsync(AuditLog.Record(AuditAction.UserLoggedIn), ct);

        return new LoginResult(_jwt.Generate(user), rawToken, user.Role.ToString());
    }

    internal static bool RequiresMfa(UserRole role) => role is
        UserRole.Administrator or UserRole.SecurityOfficer or
        UserRole.SuperAdministrator or UserRole.SecurityAdministrator or
        UserRole.SystemAdministrator or UserRole.InvestigationOfficer or
        UserRole.Auditor;

    internal static string GenerateRawToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    internal static string HashToken(string raw) =>
        Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));
}
