using APKonsult.CommandChecks;
using APKonsult.CommandChecks.Attributes;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Metadata;
using DSharpPlus.Entities;
using Serilog;

namespace APKonsult.Commands.Admin;

public static class RedirectionCommand
{
    const string REDIRECT_MESSAGE_PATH = $"{ChannelIDs.FILE_ROOT}/dm_redirection.txt";

    private static string? _responseBody;

    static RedirectionCommand()
    {
        if (!File.Exists(REDIRECT_MESSAGE_PATH))
        {
            Log.Warning("No redirection message found!");
            return;
        }

        _responseBody = File.ReadAllText(REDIRECT_MESSAGE_PATH);
    }

    [Command("issue"),
        InteractionInstallType(DiscordApplicationIntegrationType.UserInstall),
        InteractionAllowedContexts(DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel),
        RequireBotOwner]
    public static async ValueTask DmRedirectAsync(SlashCommandContext ctx)
    {
        if (_responseBody is null)
        {
            await ctx.RespondAsync("No response body has been set.", true);
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithDescription(_responseBody)
            .WithDefaultColor()
            .MakeWide();

        await ctx.RespondAsync(embed);
    }

    [Command("setissue"),
        InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall),
        InteractionAllowedContexts(DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel),
        RequireBotOwner]
    public static async ValueTask SetRedirectMessageAsync(CommandContext ctx, [RemainingText] string message)
    {
        if (Shared.TryRemoveCodeBlock(message, CodeType.All, out string? formattedMessage))
        {
            message = formattedMessage;
        }

        _responseBody = message;
        await SaveBodyAsync();

        await ctx.RespondAsync("Response body updated.");
    }

    private static async Task SaveBodyAsync()
    {
        await File.WriteAllTextAsync(REDIRECT_MESSAGE_PATH, _responseBody);
    }
}
