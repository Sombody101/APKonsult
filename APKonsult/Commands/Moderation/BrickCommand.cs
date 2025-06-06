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

        await BrickMemberAsync(ctx, member, null, reason);
    }

    public static async Task BrickTheLittleShitAsync(
        CommandContext ctx,

        [Description("The ID of the wanted user.")]
        ulong userId,

        [Description($"The reason for the timeout. Defaults to '{DEFAULT_TIMEOUT_MESSAGE}'.")]
        string reason = default!)
    {
        DiscordMember member = await ctx.Guild?.GetMemberAsync(userId)!;

        if (member is null)
        {
            await ctx.RespondAsync("Failed to find any user with that ID!");
            return;
        }

        await BrickMemberAsync(ctx, member, null, reason);
    }

    private static async Task BrickMemberAsync(CommandContext ctx, DiscordMember member, DateTime? duration, string? message)
    {
        duration ??= DateTime.Now.AddHours(1);
        message ??= FUNNI_BRICK_THROW_URL;

        await member.TimeoutAsync(duration, message);

        await ctx.Channel.SendMessageAsync("get bricked, kid");
        await ctx.Channel.SendMessageAsync(message);
    }
}
