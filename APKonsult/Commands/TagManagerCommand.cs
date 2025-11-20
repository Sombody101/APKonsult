using APKonsult.Context;
using APKonsult.Models.Main;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;

namespace APKonsult.Commands;

[Command("alias")]
public class AliasManagerCommand
{
    private readonly APKonsultContext _dbContext;

    public AliasManagerCommand(APKonsultContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Command("set"), TextAlias("add"), DefaultGroupCommand]
    public async Task SetAliasAsync(CommandContext ctx, string alias_name, [RemainingText] string alias_content)
    {
        if (alias_name.StartsWith('$'))
        {
            await ctx.RespondAsync("You cannot have an alias name start with a dollar sign ($)!");
            return;
        }

        UserDbEntity user = await _dbContext.FindOrCreateDbUserAsync(ctx.User);

        MessageTag? found_alias = await _dbContext.Set<MessageTag>().Where(tag => tag.Name == alias_name && tag.UserId == ctx.User.Id)
            .FirstOrDefaultAsync();

        if (found_alias is null)
        {
            _ = user.MessageAliases.Add(new()
            {
                User = user,
                Data = alias_content,
                Name = alias_name,
                UserId = ctx.User.Id
            });

            _ = await _dbContext.SaveChangesAsync();
            await ctx.RespondAsync($"Created alias `{alias_name}`!");
            return;
        }

        found_alias.Data = alias_content;

        _ = await _dbContext.SaveChangesAsync();
        await ctx.RespondAsync($"Updated alias `{alias_name}`!");
    }

    [Command("remove"), TextAlias("delete")]
    public async Task RemoveAliasAsync(CommandContext ctx, string alias_name)
    {
        if (alias_name.StartsWith('$'))
        {
            await ctx.RespondAsync("You cannot have an alias name start with a dollar sign ($)!");
            return;
        }

        UserDbEntity user = await _dbContext.FindOrCreateDbUserAsync(ctx.User);

        IQueryable<MessageTag> user_tags = _dbContext.Set<MessageTag>().Where(tag => tag.UserId == ctx.User.Id);

        if (!await user_tags.AnyAsync())
        {
            await ctx.RespondAsync("You don't have any aliases set!");
            return;
        }

        MessageTag? alias = await user_tags.Where(tag => tag.Name == alias_name).FirstOrDefaultAsync();

        if (alias is null)
        {
            await ctx.RespondAsync($"You don't have an alias by the name of '{alias_name}`!");
            return;
        }

        _ = user.MessageAliases.Remove(alias);
        _ = await _dbContext.SaveChangesAsync();
        await ctx.RespondAsync($"Removed `{alias_name}`");
    }

    [Command("list")]
    public async Task ListAliasesAsync(CommandContext ctx)
    {
        UserDbEntity user = await _dbContext.FindOrCreateDbUserAsync(ctx.User);

        if (!await user.MessageAliases.AnyAsync())
        {
            return;
        }

        MessageTag[] set_aliases = await _dbContext.Set<MessageTag>().Where(tag => tag.UserId == ctx.User.Id).ToArrayAsync();

        if (set_aliases.Length is 0)
        {
            await ctx.RespondAsync("You don't have any set aliases!");
            return;
        }

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle("Your aliases");

        foreach (MessageTag? alias in set_aliases)
        {
            _ = embed.AddField(alias.Name, $"{alias.Data.Length} bytes long");
        }

        await ctx.RespondAsync(embed);
    }
}