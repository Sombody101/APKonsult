using APKonsult.Context;
using APKonsult.Models.Main;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;

namespace APKonsult.CommandChecks;

public class EnsureDBEntitiesCheck : IContextCheck<UnconditionalCheckAttribute>
{
    private readonly IDbContextFactory<APKonsultContext> _contextFactory;

    public EnsureDBEntitiesCheck(IDbContextFactory<APKonsultContext> dbContextFactory)
    {
        _contextFactory = dbContextFactory;
    }

    public async ValueTask<string?> ExecuteCheckAsync(UnconditionalCheckAttribute __, CommandContext context)
    {
        DiscordUser user = context.User;

        await using APKonsultContext dbContext = await _contextFactory.CreateDbContextAsync();

        UserDbEntity userdbEntity = new(user);

        _ = await dbContext.Users.Upsert(userdbEntity)
            .On(x => x.Id)
            .NoUpdate()
            .RunAsync();

        if (context.Guild is null)
        {
            return null;
        }

        GuildDbEntity guildDbEntity = new(context.Guild);

        _ = await dbContext.Guilds.Upsert(guildDbEntity)
            .On(x => x.Id)
            .NoUpdate()
            .RunAsync();

        _ = await dbContext.SaveChangesAsync();
        return null;
    }
}