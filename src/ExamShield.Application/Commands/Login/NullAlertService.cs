using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;

namespace ExamShield.Application.Commands.Login;

internal sealed class NullAlertService : IAlertService
{
    public static readonly NullAlertService Instance = new();
    private NullAlertService() { }
    public Task SendAsync(AlertType type, string message, CancellationToken ct = default) => Task.CompletedTask;
}
