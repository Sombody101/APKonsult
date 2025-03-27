using APKonsult.Context;
using APKonsult.Models;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace APKonsult.Commands;

[Command("afk")]
public class AfkCommand
{
    private readonly APKonsultContext _dbContext;

    public AfkCommand(APKonsultContext _db)
    {
        _dbContext = _db;
    }

    [Command("set"), DefaultGroupCommand]
    public async ValueTask SetAfkStatusAsync(CommandContext ctx, [RemainingText][MinMaxLength(0, 70)] string status)
    {
        AfkStatusEntity? afkStatus = await _dbContext.Set<AfkStatusEntity>().FirstOrDefaultAsync(stat => stat.UserId == ctx.User.Id);

        if (afkStatus is not null)
        {
            return;
        }

        UserDbEntity dbuser = await _dbContext.FindOrCreateDbUserAsync(ctx.User);

        afkStatus = new AfkStatusEntity()
        {
            AfkEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            AfkMessage = status,
        };

        dbuser.AfkStatus = afkStatus;
        _ = await _dbContext.SaveChangesAsync();
        await ctx.RespondAsync($"I've set your AFK status: {status}");
    }

    public async ValueTask ListAfkUsers(CommandContext ctx, [Optional] ulong? guild_id)
    {
        guild_id ??= ctx.Guild?.Id;

        if (guild_id is null)
        {
            await ctx.RespondAsync("Error fetching guild! Try again later!");
            return;
        }

        GuildDbEntity? dbGuild = await _dbContext.Set<GuildDbEntity>().FirstOrDefaultAsync(guild => guild.Id == guild_id);

        if (dbGuild is null)
        {
            await ctx.RespondAsync("That guild is not in my database!\nAm I a member of it?");
            return;
        }

        // var afkUsers = _dbContext.Set<UserDbEntity>().Where(user => user.Id == dbGuild.);
    }
}