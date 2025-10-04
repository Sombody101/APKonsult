using APKonsult.CommandChecks.Attributes;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Entities;

namespace APKonsult.Commands.Admin;

public static class UserManager
{
    [Command("rename"), RequireAdminUser]
    public static async Task RenameUserAsync(CommandContext ctx, DiscordUser user, [RemainingText] string name)
    {
        if (ctx.Member is null)
        {
            await ctx.RespondAsync("Failed to find you in the guilds cache!");
            return;
        }

        if (user is not DiscordMember target)
        {
            await ctx.RespondAsync("Failed to find your target in the guilds cache!");
            return;
        }

        if (ctx.Member.Hierarchy < target.Hierarchy)
        {
            await ctx.RespondAsync("This member outranks you!");
            return;
        }
        else if (ctx.Member.Hierarchy == target.Hierarchy && ctx.Member.Id != target.Id)
        {
            await ctx.RespondAsync("This member has the same rank as you!");
            return;
        }

        try
        {
            await target.ModifyAsync(change => change.Nickname = name);
        }
        catch
        {
            await ctx.RespondAsync("Failed to rename that member!\nIs their rank higher than mine?");
            return;
        }

        await ctx.RespondAsync($"{(target.Nickname == string.Empty
            ? target.Username
            : target.Nickname)} has been renamed to {name}");
    }

    public static async Task RenameUserAsync(CommandContext ctx, ulong userid, [RemainingText] string name)
    {
        DiscordUser? user = await ctx.Client.GetUserAsync(userid);

        if (user is null)
        {
            await ctx.RespondAsync("Failed to find that user!");
            return;
        }

        await RenameUserAsync(ctx, user, name);
    }
}