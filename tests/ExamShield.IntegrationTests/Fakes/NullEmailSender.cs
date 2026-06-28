using ExamShield.Application.Interfaces;

namespace ExamShield.IntegrationTests.Fakes;

public sealed class NullEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
        => Task.CompletedTask;
}
