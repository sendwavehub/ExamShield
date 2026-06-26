using System.Net.Http.Json;
using ExamShield.Domain.Enums;

namespace ExamShield.Infrastructure.Alerts;

public sealed class SlackAlertChannel : IAlertChannel
{
    private readonly HttpClient _httpClient;
    private readonly string _webhookUrl;

    public SlackAlertChannel(HttpClient httpClient, string webhookUrl)
    {
        _httpClient = httpClient;
        _webhookUrl = webhookUrl;
    }

    public async Task SendAsync(AlertType type, string message, CancellationToken ct = default)
    {
        var payload = new
        {
            text = $":warning: *ExamShield Alert* — `{type}`\n{message}"
        };

        try
        {
            await _httpClient.PostAsJsonAsync(_webhookUrl, payload, ct);
        }
        catch
        {
            // Alert channels must never throw.
        }
    }
}
