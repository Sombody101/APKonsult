using Newtonsoft.Json;

namespace APKonsult.Configuration;

internal class BotConfigModel : IBotConfigModel
{
    public const ulong DebugChannel =
#if DEBUG
        ChannelIDs.CHANNEL_DEBUG; // bot-testing-debug
#else
        ChannelIDs.CHANNEL_RELEASE; // bot-testing-release
#endif

    [JsonRequired]
    [JsonProperty("bot_token")]
    public string BotToken { get; init; } = string.Empty;

    [JsonRequired]
    [JsonProperty("bot_token_debug")]
    public string DebugBotToken { get; init; } = string.Empty;

    [JsonRequired]
    [JsonProperty("command_prefixes")]
    public List<string> CommandPrefixes { get; init; } = [];

    [JsonProperty("webhook_url")]
    public string DiscordWebhookUrl { get; init; } = string.Empty;

    [JsonProperty("repl_url")]
    public string ReplUrl { get; init; } = Program.DebugBuild
        ? "http://server.lan:31337/eval" // Connect to server from dev machine
        : "http://localhost:31337/eval"; // Running from server
}