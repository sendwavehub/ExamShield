using System.Collections.Concurrent;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;

namespace ExamShield.IntegrationTests.Fakes;

public sealed class NullAlertService : IAlertService
{
    private readonly ConcurrentBag<(AlertType Type, string Message)> _sent = new();

    public IReadOnlyCollection<(AlertType Type, string Message)> SentAlerts => [.. _sent];

    public Task SendAsync(AlertType type, string message, CancellationToken ct = default)
    {
        _sent.Add((type, message));
        return Task.CompletedTask;
    }
}
