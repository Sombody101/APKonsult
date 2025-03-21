using APKonsult.CommandChecks.Attributes;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;

namespace APKonsult.CommandChecks;

public class RequireOwnerCheck : IContextCheck<RequireBotOwnerAttribute>
{
    public ValueTask<string?> ExecuteCheckAsync(RequireBotOwnerAttribute attribute, CommandContext context)
    {
        if (!IsOwner(context))
            return ValueTask.FromResult<string?>("You need to be a bot owner!");

        return ValueTask.FromResult<string?>(null);
    }

    public static bool IsOwner(CommandContext context)
    {
        var app = context.Client.CurrentApplication;
        var me = context.Client.CurrentUser;

        bool isOwner = app is not null
            ? app!.Owners!.Any(x => x.Id == context.User.Id)
            : context.User.Id == me.Id;

        return isOwner;
    }
}