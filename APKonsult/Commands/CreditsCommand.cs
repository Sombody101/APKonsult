﻿using APKonsult.CommandChecks.Attributes;
using APKonsult.Context;
using APKonsult.Models;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;

namespace APKonsult.Commands;

public class CreditsCommand
{
    private readonly APKonsultContext _dbContext;

    public CreditsCommand(APKonsultContext _db)
    {
        _dbContext = _db;
    }

    [Command("credits"), TextAlias("credit")]
    public async ValueTask ShowCreditsAsync(CommandContext ctx)
    {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithTitle("Credits")
                .WithColor(new DiscordColor(0x00ccff));

        _ = embed.AddField("Bot Daddy", GetUserMention(ctx, MadeByAttribute.Me), true)
            .AddField("Codebase & Info Commands", GetUserMention(ctx, MadeByAttribute.Lunar), true)
            .AddField("Database Layout & Host Services", GetUserMention(ctx, MadeByAttribute.Plerx), true)
            .AddField("Regex Emotional Support", GetUserMention(ctx, MadeByAttribute.Velvet), true);

        _ = embed.AddField("Bot Testers", string.Join('\n', _dbContext.Set<UserDbEntity>()
            .Where(user => user.IsBotAdmin)
            .Select(user => GetUserMention(ctx, user.Id))), true);

        _ = embed.WithDescription("Check the bot progress at the (GitHub)[https://github.com/Sombody101/APKonsult.git] page!");

        await ctx.RespondAsync(embed);
    }

    private static string GetUserMention(CommandContext ctx, ulong id)
    {
        DiscordUser user = ctx.Client.GetUserAsync(id).Result;
        return $"**{user.Username}**({id})";
    }
}