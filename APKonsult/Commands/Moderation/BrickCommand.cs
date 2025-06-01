using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using System.ComponentModel;

namespace APKonsult.Commands.Moderation;

public static class BrickCommand
{
    private const string FUNNI_BRICK_THROW_URL = "https://tenor.com/view/clonk-hooplah-brick-spongebob-noisy-gif-17264229";
    private const string DEFAULT_TIMEOUT_MESSAGE = "get bricked";

    [Command("brick"), 
        Description("Bricks the specified user and times them out with an optional message."), 
        RequirePermissions(DiscordPermission.ModerateMembers)]
    public static async Task BrickTheLittleShitAsync(
        CommandContext ctx,

        [Description("The @ of the wanted user.")]
        DiscordUser user,

        [Description($"The reason for the timeout. Defaults to '{DEFAULT_TIMEOUT_MESSAGE}'.")]
        string reason = default!)
    {
        DiscordMember member = user as DiscordMember ?? await ctx.Guild?.GetMemberAsync(user.Id)!;

        if (member is null)
        {
            return;
        }

        await member.TimeoutAsync(DateTime.Now.AddHours(1), reason);

        await ctx.Channel.SendMessageAsync("get bricked, kid");
        await ctx.Channel.SendMessageAsync(FUNNI_BRICK_THROW_URL);
    }
}
