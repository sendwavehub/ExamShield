using ExamShield.Domain.Enums;

namespace ExamShield.Infrastructure.Alerts;

public sealed class EmailAlertChannel(ISmtpEmailSender smtp, string from, string to) : IAlertChannel
{
    public async Task SendAsync(AlertType type, string message, CancellationToken ct = default)
    {
        try
        {
            await smtp.SendAsync(
                to: to,
                from: from,
                subject: $"[ExamShield Alert] {type}",
                body: $"Alert Type: {type}\n\n{message}\n\nTimestamp: {DateTimeOffset.UtcNow:u}",
                ct: ct);
        }
        catch
        {
            // Alert channels must never throw.
        }
    }
}
