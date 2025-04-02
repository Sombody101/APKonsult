using APKonsult.Context;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using System.ComponentModel;

namespace APKonsult.Commands;

[Command("react")]
public class UserReactionCommand
{
    private readonly APKonsultContext _dbContext;

    public UserReactionCommand(APKonsultContext dbcontext)
    {
        _dbContext = dbcontext;
    }

    [Command("add"), DefaultGroupCommand]
    public async Task AddReactionAsync(CommandContext ctx, DiscordEmoji emoji)
    {
        Models.UserDbEntity user = await _dbContext.FindOrCreateDbUserAsync(ctx.User);
        string emoji_name = emoji.GetDiscordName();

        if (user.ReactionEmoji == emoji_name)
        {
            await ctx.RespondAsync("You already have that emoji set!");
            return;
        }

        user.ReactionEmoji = emoji_name;
        _ = await _dbContext.SaveChangesAsync();
        await ctx.RespondAsync($"Now reacting with {emoji.Name} (`{emoji.GetDiscordName()}`)");
    }

    [Command("clear"), Description("Clears the reaction emoji (AKA: Stops the reactions)")]
    public async Task RemoveReactionAsync(CommandContext ctx)
    {
        Models.UserDbEntity user = await _dbContext.FindOrCreateDbUserAsync(ctx.User);

        if (user.ReactionEmoji == string.Empty)
        {
            await ctx.RespondAsync("You don't have any emoji set!");
            return;
        }

        user.ReactionEmoji = string.Empty;
        _ = await _dbContext.SaveChangesAsync();
        await ctx.RespondAsync("Reaction emoji cleared!");
    }
}