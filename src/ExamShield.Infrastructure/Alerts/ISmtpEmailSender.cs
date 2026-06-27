namespace ExamShield.Infrastructure.Alerts;

public interface ISmtpEmailSender
{
    Task SendAsync(string to, string from, string subject, string body, CancellationToken ct = default);
}
