using ExamShield.Application.Commands.Login;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.Login;

public sealed class LoginMfaEnforcementTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ISecurityEventRepository _security = Substitute.For<ISecurityEventRepository>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();

    private LoginCommandHandler MakeSut(bool enforce) =>
        new(_users, _hasher, _jwt, _refreshTokens, _security, _auditLog,
            new LoginOptions { EnforceMfaForPrivilegedRoles = enforce });

    private User MakeUser(UserRole role)
    {
        var user = User.Create(new Email("u@examshield.io"), "$2a$04$hash", role);
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), user.PasswordHash).Returns(true);
        return user;
    }

    [Theory]
    [InlineData(UserRole.Administrator)]
    [InlineData(UserRole.SecurityOfficer)]
    [InlineData(UserRole.SuperAdministrator)]
    [InlineData(UserRole.SecurityAdministrator)]
    [InlineData(UserRole.SystemAdministrator)]
    [InlineData(UserRole.InvestigationOfficer)]
    [InlineData(UserRole.Auditor)]
    public async Task Handle_PrivilegedRoleNoMfa_WhenEnforcementOn_ReturnsMfaSetupRequired(UserRole role)
    {
        MakeUser(role);
        var sut = MakeSut(enforce: true);

        var result = await sut.Handle(new LoginCommand("u@examshield.io", "pw"), default);

        result.MfaSetupRequired.Should().BeTrue();
        result.Token.Should().BeEmpty();
        result.RefreshToken.Should().BeEmpty();
    }

    [Theory]
    [InlineData(UserRole.Invigilator)]
    [InlineData(UserRole.Student)]
    [InlineData(UserRole.ManualReviewer)]
    public async Task Handle_NonPrivilegedRole_WhenEnforcementOn_IssuesTokenDirectly(UserRole role)
    {
        MakeUser(role);
        _jwt.Generate(Arg.Any<User>()).Returns("jwt");
        var sut = MakeSut(enforce: true);

        var result = await sut.Handle(new LoginCommand("u@examshield.io", "pw"), default);

        result.Token.Should().Be("jwt");
        result.MfaSetupRequired.Should().BeFalse();
    }

    [Theory]
    [InlineData(UserRole.Administrator)]
    [InlineData(UserRole.SuperAdministrator)]
    public async Task Handle_PrivilegedRoleNoMfa_WhenEnforcementOff_IssuesTokenDirectly(UserRole role)
    {
        MakeUser(role);
        _jwt.Generate(Arg.Any<User>()).Returns("jwt");
        var sut = MakeSut(enforce: false);

        var result = await sut.Handle(new LoginCommand("u@examshield.io", "pw"), default);

        result.Token.Should().Be("jwt");
        result.MfaSetupRequired.Should().BeFalse();
    }
}
