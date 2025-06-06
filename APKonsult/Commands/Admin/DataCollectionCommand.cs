using APKonsult.CommandChecks.Attributes;
using APKonsult.Context;
using APKonsult.Models;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using System.Text;

namespace APKonsult.Commands.Admin;

/// <summary>
/// This isn't actually data collection, just user management in the DB.
/// So maybe it is data collection then...
/// </summary>
[Command("db"), RequireBotOwner]
public sealed class DataCollectionCommand(APKonsultContext _dbContext)
{
    [Command("sync"), RequireBotOwner]
    public async Task SyncGuildUsersAsync(CommandContext ctx)
    {
        if (ctx.Guild is null)
        {
            await ctx.RespondAsync("Guild is null.");
            return;
        }

        DbSet<UserDbEntity> dbUsers = _dbContext.Set<UserDbEntity>();

        List<ulong> dbUserIds = await dbUsers.Select(u => u.Id).ToListAsync();

        IAsyncEnumerable<DiscordMember> newGuildUsers = ctx.Guild.GetAllMembersAsync()
            .Where(gm => !dbUserIds.Contains(gm.Id));

        int newCount = 0;
        await foreach (DiscordMember? user in newGuildUsers)
        {
            UserDbEntity dbUser = new(user);

            _ = await dbUsers.AddAsync(dbUser);
            newCount++;

            Log.Information("Creating new DB user {{id:{Id}, name:{Username}}}", user.Id, user.Username);
        }

        _ = await _dbContext.SaveChangesAsync();

        await ctx.RespondAsync($"Found and added {newCount}.");
    }

    [Command("collect")]
    public async Task CollectGuildUsersAsync(CommandContext ctx)
    {
        if (ctx.Guild is null)
        {
            await ctx.RespondAsync("Guild is null.");
            return;
        }

        List<DiscordMember> allGuildMembers = await ctx.Guild.GetAllMembersAsync().ToListAsync();

        List<UserDbEntity> allDbUsers = await _dbContext.Set<UserDbEntity>().ToListAsync();

        var commonUsers = allGuildMembers.Join(
            allDbUsers,
            guildMember => guildMember.Id,
            dbUser => dbUser.Id,
            (DiscordMember guildMember, UserDbEntity dbUser) => new { GuildMember = guildMember, DbEntity = dbUser }
        );

        int updateCount = 0;
        foreach (var pair in commonUsers)
        {
            pair.DbEntity.UpdateUser(pair.GuildMember);
            updateCount++;
        }

        _ = await _dbContext.SaveChangesAsync();

        await ctx.RespondAsync($"Found and updated {updateCount}.");
    }

    [Command("download"), RequireBotOwner]
    public async Task GetUserHistoryAsync(CommandContext ctx)
    {
        DbSet<UserDbEntity> dbUsers = _dbContext.Set<UserDbEntity>();

        string usersJson = JsonConvert.SerializeObject(dbUsers, Formatting.Indented);
        DiscordMessageBuilder message = new DiscordMessageBuilder()
            .WithContent("Here's a file containing some text:")
            .AddFile($"users-{Program.BUILD_TYPE.ToLower()}.json", new MemoryStream(Encoding.UTF8.GetBytes(usersJson)));

        DiscordDmChannel dmChannel = await ctx.User.CreateDmChannelAsync();
        _ = await dmChannel.SendMessageAsync(message);
    }
}
