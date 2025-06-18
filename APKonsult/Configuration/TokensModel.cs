using Newtonsoft.Json;

namespace APKonsult.Configuration;

internal sealed record TokensModel
{
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
    public string DiscordWebhookUrl { get; init; } = string.Empty;

    [JsonProperty("lavalink_password")]
    public string LavaLinkPassword { get; init; } = string.Empty;

    public override string ToString()
    {
        // Not that it would help with security a lot
        return "{}";
    }
}
