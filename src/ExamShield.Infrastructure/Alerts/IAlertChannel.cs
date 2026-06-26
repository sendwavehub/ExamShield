using ExamShield.Domain.Enums;

namespace ExamShield.Infrastructure.Alerts;

public interface IAlertChannel
{
    Task SendAsync(AlertType type, string message, CancellationToken ct = default);
}
