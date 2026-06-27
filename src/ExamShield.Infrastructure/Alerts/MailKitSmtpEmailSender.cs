using MailKit.Net.Smtp;
using MimeKit;

namespace ExamShield.Infrastructure.Alerts;

public sealed class MailKitSmtpEmailSender(
    string host, int port, string username, string password, bool useSsl) : ISmtpEmailSender
{
    public async Task SendAsync(
        string to, string from, string subject, string body, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, useSsl, ct);
        if (!string.IsNullOrEmpty(username))
            await client.AuthenticateAsync(username, password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(quit: true, ct);
    }
}
