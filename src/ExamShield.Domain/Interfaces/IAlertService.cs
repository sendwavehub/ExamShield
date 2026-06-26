using ExamShield.Domain.Enums;

namespace ExamShield.Domain.Interfaces;

public interface IAlertService
{
    Task SendAsync(AlertType type, string message, CancellationToken ct = default);
}
