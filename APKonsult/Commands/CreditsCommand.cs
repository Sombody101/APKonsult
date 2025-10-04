using APKonsult.CommandChecks;
using APKonsult.CommandChecks.Attributes;
using APKonsult.Context;
using APKonsult.Models;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;

namespace APKonsult.Commands;

public class CreditsCommand(APKonsultContext _dbContext)
{
    [Command("credits"), TextAlias("credit"), UserGuildInstallable]
    public async ValueTask ShowCreditsAsync(CommandContext ctx)
    {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithTitle("Credits")
                .WithColor(new DiscordColor(0x00ccff));

        _ = embed.AddField("Bot Daddy", GetUserMention(ctx, MadeByAttribute.Me), true)
            .AddField("Codebase & Info Commands", GetUserMention(ctx, MadeByAttribute.Lunar), true)
            .AddField("Database Layout & Host Services", GetUserMention(ctx, MadeByAttribute.Plerx), true);

        var testers = _dbContext.Set<UserDbEntity>()
            .Where(user => user.IsBotAdmin)
            .Select(user => GetUserMention(ctx, user.Id));

        if (testers.Any())
        {
            _ = embed.AddField("Bot Testers", string.Join('\n', testers), true);
        }

        _ = embed.WithDescription("Check the bot progress at the (GitHub)[https://github.com/Sombody101/APKonsult.git] page!");

        await ctx.RespondAsync(embed);
    }

    private static string GetUserMention(CommandContext ctx, ulong id)
    {
        DiscordUser user = ctx.Client.GetUserAsync(id).Result;
        return $"**{user.Username}**({id})";
    }
}