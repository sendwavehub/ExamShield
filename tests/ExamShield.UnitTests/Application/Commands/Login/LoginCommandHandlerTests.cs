using ExamShield.Application.Commands.Login;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.Login;

public sealed class LoginCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ISecurityEventRepository _security = Substitute.For<ISecurityEventRepository>();
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests() =>
        _sut = new LoginCommandHandler(_users, _hasher, _jwt, _refreshTokens, _security);

    private static User MakeUser() =>
        User.Create(new Email("op@examshield.io"), "$2a$04$hash", UserRole.Operator);

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsToken()
    {
        var user = MakeUser();
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("secret", user.PasswordHash).Returns(true);
        _jwt.Generate(user).Returns("jwt-token");

        var result = await _sut.Handle(new LoginCommand("op@examshield.io", "secret"), default);

        result.Token.Should().Be("jwt-token");
        result.Role.Should().Be(UserRole.Operator.ToString());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsInvalidCredentialsException()
    {
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = () => _sut.Handle(new LoginCommand("x@y.com", "pw"), default);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_WhenPasswordWrong_ThrowsInvalidCredentialsException()
    {
        var user = MakeUser();
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("wrong", user.PasswordHash).Returns(false);

        var act = () => _sut.Handle(new LoginCommand("op@examshield.io", "wrong"), default);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_WhenUserInactive_ThrowsInvalidCredentialsException()
    {
        var user = MakeUser();
        user.Deactivate();
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("secret", user.PasswordHash).Returns(true);

        var act = () => _sut.Handle(new LoginCommand("op@examshield.io", "secret"), default);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_WhenMfaEnabled_ReturnsRequiresMfaWithNoTokens()
    {
        var user = MakeUser();
        user.SetMfaSecret("JBSWY3DPEHPK3PXP");
        user.EnableMfa();
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("secret", user.PasswordHash).Returns(true);

        var result = await _sut.Handle(new LoginCommand("op@examshield.io", "secret"), default);

        result.RequiresMfa.Should().BeTrue();
        result.Token.Should().BeEmpty();
        result.RefreshToken.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenMfaEnabled_DoesNotIssueRefreshToken()
    {
        var user = MakeUser();
        user.SetMfaSecret("JBSWY3DPEHPK3PXP");
        user.EnableMfa();
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("secret", user.PasswordHash).Returns(true);

        await _sut.Handle(new LoginCommand("op@examshield.io", "secret"), default);

        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }
}
