using APKonsult.CommandChecks.Attributes;
using APKonsult.Context;
using APKonsult.Interactivity.Moments.Pagination;
using APKonsult.Models;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace APKonsult.Commands;

/// <summary>
/// Read-only operations are open to the public, but operations that 
/// make changes to the docket are restricted to administrators.
/// </summary>
[Command("docket"),
    Description("Features or other items to be implemented in APKognito.")]
internal class DocketCommand(APKonsultContext _dbContext) : IAutoCompleteProvider
{
    /*
     * Write commands (require permissions)
     */

    [Command("add"),
        TextAlias("new"),
        Description("Adds a new item to the docket."),
        RequireAdminUser]
    public async ValueTask AddDocket(
        CommandContext ctx,

        [Description()]
        string name,

        string description)
    {
        var docket = await GetDocketItems(ctx.Guild);

        if (docket.Exists(d => d.Name == name))
        {
            await ctx.RespondAsync($"There is already a docket item with the name '{name}'!");
            return;
        }


    }

    [Command("remove"),
        TextAlias("delete", "rm"),
        Description("Removes an item from the docket."),
        RequireAdminUser]
    public async ValueTask RemoveDocket(CommandContext ctx)
    {
        // Method intentionally left empty.
    }

    /*
     * Read commands (no permissions)
     */

    [Command("list"),
        TextAlias("get"),
        Description("Lists docket items.")]
    public async ValueTask ListDocketAsync(
        CommandContext ctx,

        [Description("The ID of the specific docket item wanted.")]
        int id = -1)
    {
        List<DocketItemEntity>? docketItems = await GetDocketItems(ctx.Guild);

        if (docketItems is null)
        {
            await ctx.RespondAsync("The docket is empty!");
            return;
        }

        if (id is not -1)
        {
            DocketItemEntity? item = docketItems.Find(d => d.Id == (ulong)id);

            if (item is null)
            {
                await ctx.RespondAsync($"Failed to find a docket item with the ID {id}.");
                return;
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithTitle($"{item.Name} (#{id})")
                .WithDescription(item.Description)
                .AddField("Status", item.Status.ToString());

            if (item.Status is not DocketItemStatus.Open)
            {
                _ = embed.AddField("Reason for closing", item.CloseReason);
            }

            await ctx.RespondAsync(embed);
            return;
        }

        await ctx.PaginateAsync(GetFormattedDocket(docketItems));
    }

    private static IEnumerable<Page> GetFormattedDocket(List<DocketItemEntity> docket)
    {
        const int LIMIT = 10;
        int count = 0;

        DiscordEmbedBuilder embed = new();

        foreach (DocketItemEntity item in docket)
        {
            count++;

            _ = embed.AddField($"{item.Name} (#{item.Id}, {item.Status})", $"{item.Description} {(string.IsNullOrWhiteSpace(item.CloseReason)
                    ? string.Empty
                    : item.CloseReason
            )}");

            if (count is LIMIT)
            {
                yield return new(new DiscordMessageBuilder().AddEmbed(embed));

                embed = new();
                count = 0;
            }
        }

        if (count is not LIMIT)
        {
            yield return new(new DiscordMessageBuilder().AddEmbed(embed));
        }
    }

    private async Task<List<DocketItemEntity>?> GetDocketItems(DiscordGuild guild)
    {
        GuildDbEntity? dbGuild = await _dbContext.Guilds
            .Include(g => g.Docket)
            .FirstOrDefaultAsync(g => g.Id == guild.Id);

        return dbGuild?.Docket;
    }

    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        bool showAll = string.IsNullOrWhiteSpace(context.UserInput);

        ulong scanId = 0;
        if (!showAll && ulong.TryParse(context.UserInput, out scanId))
        {
            scanId = 0;
        }

        List<DocketItemEntity>? docketItems = await GetDocketItems(context.Guild);

        return docketItems is null
            ? []
            : docketItems
                .Where(x => showAll || x.Id == scanId)
                .OrderBy(x => x.Id)
                .Select(x => new DiscordAutoCompleteChoice($"{x.Name} ({x.Id})", x.Id))
                .Take(25);
    }
}
