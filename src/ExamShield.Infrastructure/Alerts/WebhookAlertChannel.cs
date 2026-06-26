using System.Net.Http.Json;
using ExamShield.Domain.Enums;

namespace ExamShield.Infrastructure.Alerts;

public sealed class WebhookAlertChannel : IAlertChannel
{
    private readonly HttpClient _httpClient;
    private readonly string _url;

    public WebhookAlertChannel(HttpClient httpClient, string url)
    {
        _httpClient = httpClient;
        _url = url;
    }

    public async Task SendAsync(AlertType type, string message, CancellationToken ct = default)
    {
        var payload = new
        {
            alertType = type.ToString(),
            message,
            timestamp = DateTimeOffset.UtcNow
        };

        try
        {
            await _httpClient.PostAsJsonAsync(_url, payload, ct);
        }
        catch
        {
            // Alert channels must never throw — network failures are acceptable degradation.
        }
    }
}
