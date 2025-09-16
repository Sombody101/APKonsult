using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Serilog;
using Serilog.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace APKonsult.Commands.Moderation;

public static class BrickCommand
{
    private const string DEFAULT_TIMEOUT_MESSAGE = "get bricked";

    private static readonly string[] brickGifs = [
        "https://tenor.com/view/clonk-hooplah-brick-spongebob-noisy-gif-17264229", // Spongebob
        "https://media1.giphy.com/media/v1.Y2lkPTc5MGI3NjExYTd5b3o2MngwaTYxN254azJ6NHpjeTlld21kcTI0Ym10MDB5eDR1dyZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/qhUFBEIApYR0VTfC1v/giphy.gif", // Bloody brick
        "https://media.gifdb.com/brick-asian-girl-throw-hit-eosuh35q558n3dyf.gif", // Light toss
        "https://tenor.com/k9pyA1WpOp.gif", // Kat
        "https://tenor.com/ipCKJIaMzko.gif", // IDEK
        "https://media.tenor.com/iUjXDBq2odwAAAAM/ajr-brick-throw.gif", // AJR
        "https://tenor.com/l1Yt0CVkNiA.gif", // Polly dog
        "https://media0.giphy.com/media/v1.Y2lkPTc5MGI3NjExdjM0ZWE4dDA0Zjd4amh6YzhwM3Q5dWduZmc5c2M5anl0aWlhOTRlbSZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/8maQUL5HiZbYLeMqXl/giphy.gif", // Steak throw
    ];

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
        message ??= PickGif();

        try
        {
            await member.TimeoutAsync(duration, message);
        }
        catch (UnauthorizedException)
        {
            await ctx.RespondAsync("This user cannot be timed out!");
            return;
        }
        catch (Exception ex)
        {
            await ctx.RespondAsync("Failed to time this user out!");
            Log.Logger.Error(ex, "Failed to time user out.");
        }

        await ctx.Channel.SendMessageAsync("get bricked, kid");
        await ctx.Channel.SendMessageAsync(message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string PickGif()
    {
        return brickGifs[Random.Shared.Next(0, brickGifs.Length)];
    }
}
