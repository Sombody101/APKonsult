using APKonsult.CommandChecks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.Metadata;
using DSharpPlus.Entities;
using System.ComponentModel;

namespace APKonsult.Commands;

public static class WikiCommand
{
    public const string APKOGNITO_DOCS_URL = "https://apkognito.win";

    [Command("docs"),
        Description("Make APKonsult respond with a link to the APKognito docs page."),
        UserGuildInstallable,
        InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel)]
    public static async ValueTask SendWikiAsync(
        CommandContext ctx,

        DiscordMember? member = null)
    {
        string mention = member is not null
            ? $"Hi {member.Mention}!\n"
            : string.Empty;

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle("APKognito Wiki")
            .WithDescription($"{mention}The APKognito Wiki can be found [here]({APKOGNITO_DOCS_URL}).\nThe Docs is still a work in progress. Anyone is welcome to contribute via a PR.")
            .MakeWide()
            .WithDefaultColor();

        _ = await ctx.Channel.SendMessageAsync(embed);
    }
}