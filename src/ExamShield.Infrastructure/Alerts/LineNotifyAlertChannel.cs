using System.Net.Http.Headers;
using ExamShield.Domain.Enums;

namespace ExamShield.Infrastructure.Alerts;

public sealed class LineNotifyAlertChannel(HttpClient httpClient, string token) : IAlertChannel
{
    private const string Endpoint = "https://notify-api.line.me/api/notify";

    public async Task SendAsync(AlertType type, string message, CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["message"] = $"\n[ExamShield] {type}: {message}"
                })
            };
            await httpClient.SendAsync(request, ct);
        }
        catch
        {
            // Alert channels must never throw.
        }
    }
}
