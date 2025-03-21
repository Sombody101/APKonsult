using APKonsult.Models;
using DSharpPlus.Entities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;

namespace APKonsult.Context;

internal static class DbHelpers
{
    public static async ValueTask<UserDbEntity> FindOrCreateUserAsync(this APKonsultContext _dbContext, DiscordUser dUser)
    {
        UserDbEntity? user = await _dbContext.Users.FindAsync(dUser.Id);

        if (user is not null)
        {
            return user;
        }

        user = new UserDbEntity()
        {
            Username = dUser.Username,
            Id = dUser.Id,
        };

        _ = await _dbContext.Users.AddAsync(user);
        _ = await _dbContext.SaveChangesAsync();

        return user;
    }

    public static async ValueTask<GuildDbEntity> FindOrCreateGuildAsync(this APKonsultContext _dbContext, DiscordGuild guild)
    {
        GuildDbEntity? dbGuild = await _dbContext.Guilds.FindAsync(guild.Id);

        if (dbGuild is not null)
        {
            return dbGuild;
        }

        dbGuild = new GuildDbEntity(guild.Id)
        {
            Settings = new() { GuildId = guild.Id }
        };

        _ = await _dbContext.Guilds.AddAsync(dbGuild);
        _ = await _dbContext.SaveChangesAsync();

        return dbGuild;
    }
}