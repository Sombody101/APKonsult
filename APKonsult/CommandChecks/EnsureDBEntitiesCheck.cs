using APKonsult.Context;
using APKonsult.Models;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;

namespace APKonsult.CommandChecks;

public class EnsureDBEntitiesCheck : IContextCheck<UnconditionalCheckAttribute>
{
    private IDbContextFactory<APKonsultContext> _contextFactory;

    public EnsureDBEntitiesCheck(IDbContextFactory<APKonsultContext> dbContextFactory)
    {
        _contextFactory = dbContextFactory;
    }

    public async ValueTask<string?> ExecuteCheckAsync(UnconditionalCheckAttribute _, CommandContext context)
    {
        DiscordUser user = context.User;

        await using APKonsultContext dbContext = await _contextFactory.CreateDbContextAsync();

        UserDbEntity userdbEntity = new()
        {
            Id = user.Id,
            Username = user.Username,
        };

        await dbContext.Users.Upsert(userdbEntity)
            .On(x => x.Id)
            .NoUpdate()
            .RunAsync();

        if (context.Guild is null)
        {
            return null;
        }

        GuildDbEntity guildDbEntity = new(context.Guild.Id);

        await dbContext.Guilds.Upsert(guildDbEntity)
            .On(x => x.Id)
            .NoUpdate()
            .RunAsync();

        await dbContext.SaveChangesAsync();
        return null;
    }
}