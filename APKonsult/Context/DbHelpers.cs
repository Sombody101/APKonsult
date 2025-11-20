using APKonsult.Models.Main;
using DSharpPlus.Entities;
using Serilog;

namespace APKonsult.Context;

internal static class DbHelpers
{
    public static async ValueTask<UserDbEntity> FindOrCreateDbUserAsync(this APKonsultContext _dbContext, DiscordUser user)
    {
        UserDbEntity? dbUser = await _dbContext.Users.FindAsync(user.Id);

        if (dbUser is not null)
        {
            return dbUser;
        }

        dbUser = new UserDbEntity(user);

        Log.Information("Creating new DB user {{id:{Id}, name:{Username}}}", user.Id, user.Username);
        _ = await _dbContext.Users.AddAsync(dbUser);
        _ = await _dbContext.SaveChangesAsync();

        return dbUser;
    }

    public static async ValueTask<GuildDbEntity> FindOrCreateDbGuildAsync(this APKonsultContext _dbContext, DiscordGuild guild)
    {
        GuildDbEntity? dbGuild = await _dbContext.Guilds.FindAsync(guild.Id);

        if (dbGuild is not null)
        {
            return dbGuild;
        }

        dbGuild = new GuildDbEntity(guild.Id)
        {
            Settings = new()
            {
                GuildId = guild.Id
            }
        };

        Log.Information("Creating new DB guild {{id:{Id}, name:{Username}}}", guild.Id, guild.Name);
        _ = await _dbContext.Guilds.AddAsync(dbGuild);
        _ = await _dbContext.SaveChangesAsync();

        return dbGuild;
    }
}