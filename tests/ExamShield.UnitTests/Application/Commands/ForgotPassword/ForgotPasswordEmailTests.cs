using ExamShield.Application.Commands.ForgotPassword;
using ExamShield.Application.Interfaces;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.ForgotPassword;

public sealed class ForgotPasswordEmailTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordResetTokenRepository _tokens = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();

    private ForgotPasswordCommandHandler MakeSut() =>
        new(_users, _tokens, _email);

    [Fact]
    public async Task Handle_WhenUserExists_SendsResetEmail()
    {
        var user = User.Create(new Email("alice@test.io"), "hash", UserRole.Student);
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);

        await MakeSut().Handle(new ForgotPasswordCommand("alice@test.io"), default);

        await _email.Received(1).SendAsync(
            "alice@test.io",
            Arg.Is<string>(s => s.Contains("password")),
            Arg.Is<string>(s => s.Contains("reset")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_SendsNoEmail()
    {
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        await MakeSut().Handle(new ForgotPasswordCommand("nobody@test.io"), default);

        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserExists_StoresResetToken()
    {
        var user = User.Create(new Email("bob@test.io"), "hash", UserRole.Student);
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);

        await MakeSut().Handle(new ForgotPasswordCommand("bob@test.io"), default);

        await _tokens.Received(1).AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailBodyContainsResetLink()
    {
        var user = User.Create(new Email("carol@test.io"), "hash", UserRole.Student);
        _users.FindByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);

        string? capturedBody = null;
        await _email.SendAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Do<string>(b => capturedBody = b),
            Arg.Any<CancellationToken>());

        await MakeSut().Handle(
            new ForgotPasswordCommand("carol@test.io", "https://app.examshield.io/reset-password"),
            default);

        capturedBody.Should().Contain("https://app.examshield.io/reset-password");
    }
}
