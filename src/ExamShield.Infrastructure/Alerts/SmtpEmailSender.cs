using ExamShield.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ExamShield.Infrastructure.Alerts;

/// <summary>
/// Application-layer IEmailSender adapter backed by MailKit SMTP.
/// Reads SMTP settings from Smtp:* configuration section.
/// </summary>
public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var host     = configuration["Smtp:Host"]     ?? "localhost";
        var port     = int.Parse(configuration["Smtp:Port"]     ?? "25");
        var username = configuration["Smtp:Username"] ?? string.Empty;
        var password = configuration["Smtp:Password"] ?? string.Empty;
        var useSsl   = bool.Parse(configuration["Smtp:UseSsl"] ?? "false");
        var from     = configuration["Smtp:From"]     ?? "noreply@examshield.local";

        var sender = new MailKitSmtpEmailSender(host, port, username, password, useSsl);
        await sender.SendAsync(to, from, subject, body, ct);
    }
}
