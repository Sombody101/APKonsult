using Newtonsoft.Json;
using Serilog;

namespace APKonsult.Configuration;

public sealed record TokensModel
{
    public TokensModel()
    {
        string? enTtoken = Environment.GetEnvironmentVariable("WATCHTOWER_HTTP_API_TOKEN")?.Trim();

        if (string.IsNullOrWhiteSpace(enTtoken) || enTtoken.Length < 16)
        {
            Log.Warning("WATCHTOWER_HTTP_API_TOKEN is null or an invalid length. Watchtower manipulation commands will not work.");
            WatchtowerToken = string.Empty;
            return;
        }

        WatchtowerToken = enTtoken;
    }

    [JsonRequired]
    [JsonProperty("bot_token")]
    internal string BotToken { get; init; } = string.Empty;

    [JsonRequired]
    [JsonProperty("bot_token_debug")]
    internal string DebugBotToken { get; init; } = string.Empty;

    [JsonIgnore]
    internal string TargetBotToken
    {
        get
        {
#if DEBUG
            return DebugBotToken;
#else
            return BotToken;
#endif
        }
    }

    [JsonProperty("webhook_url")]
    internal string DiscordWebhookUrl { get; init; } = string.Empty;

    [JsonIgnore]
    public string WatchtowerToken { get; }

    public override string ToString()
    {
        // Not that it would help with security a lot
        return "{}";
    }
}
