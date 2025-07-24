using APKonsult.CommandChecks.Attributes;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;

namespace APKonsult.CommandChecks;

public class RequireOwnerCheck : IContextCheck<RequireBotOwnerAttribute>
{
    public ValueTask<string?> ExecuteCheckAsync(RequireBotOwnerAttribute attribute, CommandContext context)
    {
        return !IsOwner(context) 
            ? ValueTask.FromResult<string?>("You need to be a bot owner!") 
            : ValueTask.FromResult<string?>(null);
    }

    public static bool IsOwner(CommandContext context)
    {
        DSharpPlus.Entities.DiscordApplication? app = context.Client.CurrentApplication;
        DSharpPlus.Entities.DiscordUser me = context.Client.CurrentUser;

        bool isOwner = app is not null
            ? app!.Owners!.Any(x => x.Id == context.User.Id)
            : context.User.Id == me.Id;

        return isOwner;
    }
}