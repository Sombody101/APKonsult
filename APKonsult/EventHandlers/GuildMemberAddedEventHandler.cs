using APKonsult.Context;
using APKonsult.Models;
using DSharpPlus;
using DSharpPlus.EventArgs;

namespace APKonsult.EventHandlers;

public sealed class GuildMemberAddedEventHandler(APKonsultContext _dbContext) : IEventHandler<GuildMemberAddedEventArgs>
{
    public async Task HandleEventAsync(DiscordClient sender, GuildMemberAddedEventArgs eventArgs)
    {
        var userSet = _dbContext.Set<UserDbEntity>();
        if (await userSet.FindAsync(eventArgs.Member.Id) is not null)
        {
            return;
        }

        userSet.Add(new(eventArgs.Member, DateTimeOffset.UtcNow));
        await _dbContext.SaveChangesAsync();
    }
}
