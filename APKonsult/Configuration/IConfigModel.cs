namespace APKonsult.Configuration;

internal interface IBotConfigModel
{
    public string BotToken { get; }

    public List<string> CommandPrefixes { get; }

    public string DiscordWebhookUrl { get; }

    public string ReplUrl { get; }
}