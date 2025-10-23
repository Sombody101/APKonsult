using APKonsult.CommandChecks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands.Metadata;
using DSharpPlus.Entities;
using System.ComponentModel;

namespace APKonsult.Commands;

public static class PingCommand
{
    [Command("ping"),
        Description("Pings the bot and returns the gateway latency."),
        UserGuildInstallable,
        InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel)]
    public static async Task PingAsync(CommandContext ctx)
    {
        TimeSpan latency = ctx.Client.GetConnectionLatency(ctx.Guild?.Id ?? ctx.Channel.Id);

        await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
            .WithTitle(Random.Shared.Next() % 3948 == 0
                ? "Pongie!"
                : "Pong!")
            .WithDefaultColor()
            .AddField($"Response latency", $"{latency.Milliseconds}ms ({latency.TotalMilliseconds}ms)"));
    }

    [Command("uptime"),
        Description("Get the bots uptime"),
        UserGuildInstallable,
        InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel)]
    public static async Task UptimeAsync(CommandContext ctx)
    {
        await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
            .WithTitle("Uptime")
            .WithDefaultColor()
            .WithDescription(Shared.FormatTickCount()));
    }

    [Command("echo"),
        Description("Makes the bot create a message with your text"),
        UserGuildInstallable,
        InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel)]
    public static async Task EchoAsync(
        CommandContext ctx,

        [Description("The text you want APKonsult to reply with."),
            RemainingText]
        string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        await ctx.RespondAsync(message);
    }

    [Command("embed"),
        Description("The same as 'echo', but prints the text in an embed"),
        UserGuildInstallable,
        InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel)]
    public static async Task EchoEmbedAsync(
        CommandContext ctx,

        [Description("The test you want APKonsult to reply with via an embed."),
            RemainingText]
        string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        await ctx.RespondAsync(new DiscordEmbedBuilder().WithDescription(message));
    }
}