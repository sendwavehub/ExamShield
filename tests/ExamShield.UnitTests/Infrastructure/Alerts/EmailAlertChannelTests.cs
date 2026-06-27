using ExamShield.Domain.Enums;
using ExamShield.Infrastructure.Alerts;
using NSubstitute;
using FluentAssertions;
using Xunit;

namespace ExamShield.UnitTests.Infrastructure.Alerts;

public sealed class EmailAlertChannelTests
{
    private readonly ISmtpEmailSender _smtp = Substitute.For<ISmtpEmailSender>();
    private readonly EmailAlertChannel _sut;

    public EmailAlertChannelTests() =>
        _sut = new EmailAlertChannel(_smtp, from: "alerts@examshield.io", to: "admin@examshield.io");

    [Fact]
    public async Task SendAsync_DelegatesToSmtpSender()
    {
        await _sut.SendAsync(AlertType.HashMismatch, "Hash mismatch detected.");

        await _smtp.Received(1).SendAsync(
            to: "admin@examshield.io",
            from: "alerts@examshield.io",
            subject: Arg.Is<string>(s => s.Contains("HashMismatch")),
            body: Arg.Is<string>(b => b.Contains("Hash mismatch detected.")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WhenSmtpThrows_DoesNotThrow()
    {
        _smtp.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("SMTP failure")));

        await ((Func<Task>)(() => _sut.SendAsync(AlertType.HashMismatch, "msg")))
            .Should().NotThrowAsync();
    }
}
