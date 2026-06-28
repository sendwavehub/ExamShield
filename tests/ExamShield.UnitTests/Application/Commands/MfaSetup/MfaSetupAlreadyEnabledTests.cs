using ExamShield.Application.Commands.MfaSetup;
using ExamShield.Application.Interfaces;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.MfaSetup;

public sealed class MfaSetupAlreadyEnabledTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITotpService    _totp  = Substitute.For<ITotpService>();
    private readonly MfaSetupCommandHandler _sut;

    public MfaSetupAlreadyEnabledTests() =>
        _sut = new MfaSetupCommandHandler(_users, _totp);

    private static User MfaEnabledUser()
    {
        var user = User.Create(
            new Email("mfa@test.com"),
            BCrypt.Net.BCrypt.HashPassword("Pass@1234", workFactor: 4),
            ExamShield.Domain.Enums.UserRole.Operator);
        user.SetMfaSecret("OLDSECRET");
        user.EnableMfa();
        return user;
    }

    [Fact]
    public async Task Handle_WhenMfaAlreadyEnabled_ThrowsInvalidOperationException()
    {
        var user = MfaEnabledUser();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).Returns(user);

        var act = () => _sut.Handle(
            new MfaSetupCommand(Guid.NewGuid(), "mfa@test.com"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already*");
    }

    [Fact]
    public async Task Handle_WhenMfaAlreadyEnabled_DoesNotOverwriteSecret()
    {
        var user = MfaEnabledUser();
        _users.GetByIdAsync(Arg.Any<UserId>(), default).Returns(user);
        _totp.GenerateSecret().Returns("NEWSECRET");

        try
        {
            await _sut.Handle(new MfaSetupCommand(Guid.NewGuid(), "mfa@test.com"), default);
        }
        catch { /* expected */ }

        user.MfaSecret.Should().Be("OLDSECRET");
    }
}
