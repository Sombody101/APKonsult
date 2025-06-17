using Newtonsoft.Json;

namespace APKonsult.Configuration;

public sealed record BotConfigModel
{
    public const ulong DEBUG_CHANNEL =
#if DEBUG
        ChannelIDs.CHANNEL_DEBUG; // bot-testing-debug
#else
        ChannelIDs.CHANNEL_RELEASE; // bot-testing-release
#endif

    [JsonRequired]
    [JsonProperty("command_prefixes")]
    public List<string> CommandPrefixes { get; init; } = [];

    [JsonProperty("user_agent")]
    public string UserAgent { get; init; } = string.Empty;

    // This likely won't be used...
    // Just a ruminant of Lloyd.
    [JsonProperty("repl_url")]
    public string ReplUrl { get; init; } = Program.IS_DEBUG_BUILD
        ? "http://server.lan:31337/eval" // Connect to server from dev machine
        : "http://localhost:31337/eval"; // Running from server
}