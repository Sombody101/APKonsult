using APKonsult.CommandChecks.Attributes;
using APKonsult.Context;
using APKonsult.Interactivity.Moments.Pagination;
using APKonsult.Models.Main;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Humanizer;
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
    private const string DOCKET_EMPTY_TEXT = "The docket is empty!";
    private const string DOCKET_ID_DESCRIPTION = "The ID of the docket item.";

    /*
     * Write commands (require permissions)
     */

    [Command("add"),
        TextAlias("new"),
        Description("Adds a new item to the docket."),
        RequireAdminUser]
    public async ValueTask AddDocketAsync(
        CommandContext ctx,

        [Description("The name for the docket item to be added.")]
        string name,

        string description)
    {
        List<DocketItemEntity>? docket = await GetDocketItemsAsync(ctx.Guild);

        if (docket.Exists(d => d.Name == name))
        {
            await ctx.RespondAsync($"There is already a docket item with the name '{name}'!");
            return;
        }

        DocketItemEntity newItem = new()
        {
            Name = name.Titleize(),
            Description = description,
            Id = (ulong)docket.Count
        };

        docket.Add(newItem);
        await _dbContext.SaveChangesAsync();

        await ctx.RespondAsync($"Created docket item {newItem.Name} (#{newItem.Id}).");
    }

    [Command("mark"),
        Description("Mark a docket item as solved, fixed, ignored, open, etc.")]
    public async ValueTask MarkDocketItemAsync(
        CommandContext ctx,

        [SlashAutoCompleteProvider(typeof(DocketCommand)),
            Description(DOCKET_ID_DESCRIPTION)]
        ulong id,

        [SlashAutoCompleteProvider(typeof(DocketStatusAutocomplte)),
            Description("The status to set the docket item to.")]
        DocketItemStatus status)
    {
        List<DocketItemEntity> docket = await GetDocketItemsAsync(ctx.Guild);

        if (docket.Count is 0)
        {
            await ctx.RespondAsync(DOCKET_EMPTY_TEXT);
            return;
        }

        DocketItemEntity? docketItem = docket.Find(d => d.Id == id);

        if (docketItem is null)
        {
            await ctx.RespondAsync($"Failed to find #{id} on the docket!");
            return;
        }

        docketItem.Status = status;
        await _dbContext.SaveChangesAsync();

        await ctx.RespondAsync($"Set the status {FormatDocketName(docketItem)} to {status.Humanize()}");
    }

    [Command("remove"),
        TextAlias("delete", "rm"),
        Description("Removes an item from the docket."),
        RequireAdminUser]
    public async ValueTask RemoveDocketAsync(
        CommandContext ctx,

        [SlashAutoCompleteProvider(typeof(DocketCommand)),
            Description(DOCKET_ID_DESCRIPTION)]
        ulong id)
    {
        List<DocketItemEntity> docket = await GetDocketItemsAsync(ctx.Guild);

        int docketItemIndex = docket.FindIndex(d => d.Id == id);

        if (docketItemIndex is -1)
        {
            await ctx.RespondAsync($"Failed to find #{id} on the docket!");
            return;
        }

        DocketItemEntity docketItem = docket[docketItemIndex];
        docket.RemoveAt(docketItemIndex);
        await _dbContext.SaveChangesAsync();

        await ctx.RespondAsync($"Docket item {docketItem.Name} (#{docketItem.Id}) from the docket!");
    }

    /*
     * Read commands (no permissions)
     */

    [Command("list"),
        TextAlias("get"),
        Description("Lists docket items.")]
    public async ValueTask ListDocketAsync(
        CommandContext ctx,

        [SlashAutoCompleteProvider(typeof(DocketCommand)),
            Description(DOCKET_ID_DESCRIPTION)]
        int id = -1)
    {
        List<DocketItemEntity>? docketItems = await GetDocketItemsAsync(ctx.Guild);

        if (docketItems.Count is 0)
        {
            await ctx.RespondAsync(DOCKET_EMPTY_TEXT);
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

        await ctx.PaginateAsync(GetFormattedDocketList(docketItems));
    }

    private static IEnumerable<Page> GetFormattedDocketList(List<DocketItemEntity> docket)
    {
        const int LIMIT = 10;
        int count = 0;

        DiscordEmbedBuilder embed = new();

        foreach (DocketItemEntity item in docket)
        {
            count++;

            _ = embed.AddField(
                $"{item.Name} (#{item.Id}, {item.Status})",
                $"{item.Description} {(string.IsNullOrWhiteSpace(item.CloseReason)
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

    private async Task<List<DocketItemEntity>> GetDocketItemsAsync(DiscordGuild guild)
    {
        GuildDbEntity? dbGuild = await _dbContext.Guilds
            .Include(g => g.Docket)
            .OrderBy(g => g.Id)
            .FirstOrDefaultAsync(g => g.Id == guild.Id);

        return dbGuild?.Docket ?? [];
    }

    /// <summary>
    /// </summary>
    /// <param name="context"></param>
    /// <returns><see cref="DiscordAutoCompleteChoice"/>(<see cref="string"/> Name, <see cref="ulong"/> ID)</returns>
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        bool showAll = string.IsNullOrWhiteSpace(context.UserInput);

        ulong scanId = 0;
        if (!showAll && ulong.TryParse(context.UserInput, out scanId))
        {
            scanId = 0;
        }

        List<DocketItemEntity>? docketItems = await GetDocketItemsAsync(context.Guild);

        return docketItems is null
            ? []
            : docketItems
                .Where(x => showAll || x.Id == scanId)
                .OrderBy(x => x.Id)
                .Select(x => new DiscordAutoCompleteChoice($"{x.Name} ({x.Id})", x.Id))
                .Take(25);
    }

    private string FormatDocketName(DocketItemEntity dItem)
    {
        return $"{dItem.Name} (#{dItem.Id})";
    }
}

public class DocketStatusAutocomplte : IAutoCompleteProvider
{
    private static readonly Dictionary<string, DocketItemStatus> _cachedStatusTypes = Enum.GetValues<DocketItemStatus>()
        .ToDictionary(x => x.Humanize(), x => x);

    public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        bool getAll = string.IsNullOrWhiteSpace(context.UserInput);

        IEnumerable<DiscordAutoCompleteChoice> items = _cachedStatusTypes
            .Where(s => getAll || s.Key.Contains(context.UserInput, StringComparison.OrdinalIgnoreCase))
            .Select(s => new DiscordAutoCompleteChoice(s.Key, s.Value));

        return ValueTask.FromResult(items);
    }
}