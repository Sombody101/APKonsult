using DSharpPlus.Commands;
using DSharpPlus.Entities;
using System.ComponentModel;

namespace APKonsult.Commands;

internal static class WikiCommand
{
    [Command("wiki"), Description("Make APKonsult respond with a link to the APKognito Wiki page.")]
    public static async ValueTask SendWikiAsync(
        CommandContext ctx,

        DiscordMember? member = null)
    {
        string mention = member is not null
            ? $"Hi {member.Mention}!\n"
            : string.Empty;

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle("APKognito Wiki")
            .WithDescription($"{mention}The APKognito Wiki can be found [here](https://github.com/Sombody101/APKognito/wiki).\n" +
                "The Wiki is still a work in progress. Anyone is welcome to contribute via a PR.")
            .MakeWide()
            .WithColor();

        _ = await ctx.Channel.SendMessageAsync(embed);
    }
}
