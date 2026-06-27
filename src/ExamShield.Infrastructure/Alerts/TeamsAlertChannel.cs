using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ExamShield.Domain.Enums;

namespace ExamShield.Infrastructure.Alerts;

public sealed class TeamsAlertChannel(HttpClient httpClient, string webhookUrl) : IAlertChannel
{
    public async Task SendAsync(AlertType type, string message, CancellationToken ct = default)
    {
        var payload = new TeamsMessageCard
        {
            Type = "MessageCard",
            Context = "http://schema.org/extensions",
            ThemeColor = "FF0000",
            Summary = $"ExamShield Alert: {type}",
            Sections =
            [
                new TeamsSection
                {
                    ActivityTitle = $"ExamShield Alert — {type}",
                    ActivityText = message
                }
            ]
        };

        try
        {
            await httpClient.PostAsJsonAsync(webhookUrl, payload, ct);
        }
        catch
        {
            // Alert channels must never throw.
        }
    }

    private sealed class TeamsMessageCard
    {
        [JsonPropertyName("@type")] public string Type { get; init; } = "";
        [JsonPropertyName("@context")] public string Context { get; init; } = "";
        public string ThemeColor { get; init; } = "";
        public string Summary { get; init; } = "";
        public TeamsSection[] Sections { get; init; } = [];
    }

    private sealed class TeamsSection
    {
        public string ActivityTitle { get; init; } = "";
        public string ActivityText { get; init; } = "";
    }
}
