using APKonsult.Context;
using APKonsult.Models;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;

namespace APKonsult.CommandChecks;

public class UnconditionalCheck : IContextCheck<UnconditionalCheckAttribute>
{
    private readonly APKonsultContext _dbContext;

    public UnconditionalCheck(APKonsultContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ValueTask<string?> ExecuteCheckAsync(UnconditionalCheckAttribute attribute, CommandContext context)
    {
        ulong guildid = context.Guild?.Id ?? 0;
        BlacklistedDbEntity? blacklistedEntity = _dbContext.Set<BlacklistedDbEntity>()
            .Where(bl => bl.UserId == context.User.Id || bl.GuildId == guildid)
            .FirstOrDefault();

        if (blacklistedEntity is null)
        {
            // The user or guild is in good standing
            return ValueTask.FromResult<string?>(null);
        }

        return blacklistedEntity.UserId is 0
            ? ValueTask.FromResult<string?>($"This guild has been banned from APKonsult!\n{blacklistedEntity.BanReason()}")
            : ValueTask.FromResult<string?>($"You were banned from using APKonsult!\n{blacklistedEntity.BanReason()}");
    }
}