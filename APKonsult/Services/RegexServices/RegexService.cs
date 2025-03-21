using APKonsult.Context;
using APKonsult.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.RegularExpressions;

namespace APKonsult.Services.RegexServices;

public interface IRegexService
{
    ValueTask CreateRegexAsync(ulong guildId, ulong creatorId, TrackingConfigurationSummary summary);

    ValueTask UseRegexAsync(ulong guildId, ulong channelId, DiscordMessage invokingMessage);

    ValueTask ModifyRegexAsync(ulong guildId, ulong modifierId, string regexName, TrackingConfigurationSummary content);

    ValueTask DeleteRegexAsync(ulong guildId, string name);

    ValueTask<TrackingDbEntity?> GetRegexAsync(ulong guildId, string name);

    ValueTask<IReadOnlyCollection<TrackingDbEntity>> GetRegexesOwnedByGuildAsync(ulong guildId);

    ValueTask<bool> RegexExistsAsync(ulong guildId, string name);

    ValueTask RefreshCacheAsync(ulong guildId);

    ValueTask<IReadOnlyList<TrackingConfigurationBlame>> GetBlameRegexEditorsAsync(ulong guildId, string name);

    bool TrackerDisabled(TrackingDbEntity? tracker);
}

internal class RegexService : IRegexService
{
    private readonly DiscordClient _client;
    private readonly APKonsultContext _dbContext;
    private readonly IRegexCache _regexCache;

    public RegexService(DiscordClient client,
        APKonsultContext dbContext,
        IRegexCache regexCache)
    {
        _client = client;
        _dbContext = dbContext;
        _regexCache = regexCache;
    }

    /// <summary>
    /// Creates a <see cref="TrackingDbEntity"/> with the given <see cref="TrackingConfigurationSummary"/>
    /// owned by <paramref name="guildId"/>
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="creatorId"></param>
    /// <param name="name"></param>
    /// <param name="content"></param>
    /// <param name="sourceChannel"></param>
    /// <param name="reportChannel"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    async ValueTask IRegexService.CreateRegexAsync(ulong guildId, ulong creatorId, TrackingConfigurationSummary summary)
    {
        if (string.IsNullOrWhiteSpace(summary.Name))
            throw new ArgumentException("The tracker name cannot be blank or whitespace.", nameof(summary));

        summary.Name = summary.Name.Trim();

        if (await _dbContext.Set<TrackingDbEntity>().Where(x => x.GuildId == guildId).AnyAsync(x => x.Name == summary.Name))
            throw new InvalidOperationException($"A tracker with the name '{summary.Name}' already exists.");

        var epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var regex = new TrackingDbEntity()
        {
            GuildId = guildId,
            Name = summary.Name,
            RegexPattern = summary.RegexPattern,
            SourceChannelId = summary.SourceChannelId,
            ReportChannelId = summary.ReportChannelId,
            EditorList = $"{creatorId}:{epoch}:0;", // Initialize with one editor and no change (creation implies all are changed)
            CreationEpoch = epoch
        };

        await _dbContext.Set<TrackingDbEntity>().AddAsync(regex);
        await _dbContext.SaveChangesAsync();

        _regexCache.Add(guildId, summary.Name);
    }

    /// <summary>
    /// Removes the <see cref="TrackingDbEntity"/> based on the given <paramref name="guildId"/> and <paramref name="name"/>
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    async ValueTask IRegexService.DeleteRegexAsync(ulong guildId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The tracker name cannot be blank or whitespace.", nameof(name));

        name = name.Trim();

        var regex = await _dbContext
            .Set<TrackingDbEntity>()
            .Where(x => x.GuildId == guildId && x.Name == name)
            .SingleOrDefaultAsync() ?? throw new ArgumentException("The tracker name provided was not found.");

        _dbContext.Remove(regex);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Modify one or more properties of a <see cref="TrackingDbEntity"/> with a <see cref="TrackingConfigurationSummary"/>
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="modifierId"></param>
    /// <param name="regexName"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    async ValueTask IRegexService.ModifyRegexAsync(ulong guildId, ulong modifierId, string regexName, TrackingConfigurationSummary content)
    {
        if (string.IsNullOrWhiteSpace(content.Name))
            throw new ArgumentException("The tracker name cannot be blank or whitespace.", nameof(content));

        content.Name = content.Name.ToLower();

        var regex = await _dbContext
            .Set<TrackingDbEntity>()
            .Where(x => x.GuildId == guildId && x.Name == regexName)
            .SingleOrDefaultAsync() ?? throw new ArgumentException("The tracker name profided was not found.");

        var changes = TrackingConfigurationChange.None;

        if (regex.Name != content.Name)
            changes |= TrackingConfigurationChange.NameChange;

        if (regex.SourceChannelId != content.SourceChannelId)
            changes |= TrackingConfigurationChange.SourceChannelChange;

        if (regex.ReportChannelId != content.ReportChannelId)
            changes |= TrackingConfigurationChange.ReportChannelChange;

        if (regex.RegexPattern != content.RegexPattern)
            changes |= TrackingConfigurationChange.RegexPatternChange;

        // Modify the saved regex (and update the editor list)
        regex.EditorList = $"{regex.EditorList}{modifierId}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:{changes:d};";
        regex.Name = content.Name;
        regex.SourceChannelId = content.SourceChannelId;
        regex.ReportChannelId = content.ReportChannelId;
        regex.RegexPattern = content.RegexPattern;

        await _dbContext.SaveChangesAsync();
    }

    async ValueTask<TrackingDbEntity?> IRegexService.GetRegexAsync(ulong guildId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The tracker name cannot be blank or whitespace.", nameof(name));

        name = name.Trim();

        return await _dbContext
            .Set<TrackingDbEntity>()
            .Where(x => x.GuildId == guildId && x.Name == name)
            .FirstOrDefaultAsync();
    }

    async ValueTask<IReadOnlyCollection<TrackingDbEntity>> IRegexService.GetRegexesOwnedByGuildAsync(ulong guildId)
    {
        return await _dbContext.Set<TrackingDbEntity>()
            .Where(x => x.GuildId == guildId).ToArrayAsync();
    }

    async ValueTask IRegexService.RefreshCacheAsync(ulong guildId)
    {
        var regexes = await _dbContext
            .Set<TrackingDbEntity>()
            .Where(entity => entity.GuildId == guildId)
            .Select(entity => entity.Name)
            .ToArrayAsync();

        _regexCache.Set(guildId, regexes);
    }

    async ValueTask<bool> IRegexService.RegexExistsAsync(ulong guildId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The tracker name cannot be blank or whitespace.", nameof(name));

        name = name.Trim();

        return await _dbContext
            .Set<TrackingDbEntity>()
            .Where(x => x.GuildId == guildId && x.Name == name)
            .AnyAsync();
    }

    async ValueTask IRegexService.UseRegexAsync(ulong guildId, ulong channelId, DiscordMessage invokingMessage)
    {
        var regexes = await _dbContext
            .Set<TrackingDbEntity>()
            .Where(x => x.GuildId == guildId && x.SourceChannelId == channelId)
            .ToArrayAsync();

        if (regexes is null || regexes.Length is 0)
            return; // Channel isn't configured with any trackers

        foreach (var regex in regexes)
        {
            if (regex.SourceChannelId == 0 || regex.ReportChannelId == 0)
                continue;

            var result = Regex.Match(invokingMessage.Content, regex.RegexPattern);

            if (result.Success)
            {
                var reportChannel = await _client.GetChannelAsync(regex.ReportChannelId);

                if (reportChannel is null)
                {
                    Log.Information("Failed to get the reporter channel `{ReportChannelId}` for guild `{GuildId}` ", regex.ReportChannelId, guildId);
                    return;
                }

                var timestamp = invokingMessage.Timestamp.ToUnixTimeSeconds();
                await reportChannel.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithTitle($"{regex.Name} Event")
                    .AddField("Offending Message", invokingMessage.JumpLink.ToString())
                    .AddField("Offending Content", result.Value ?? "N/A")
                    .AddField("Message Time", $"This message was sent <t:{timestamp}:R>, or <t:{timestamp}:F>"));

                break;
            }
        }
    }

    /// <summary>
    /// Returns a list of <see cref="TrackingConfigurationBlame"/> to show the edit history of a specific <see cref="TrackingDbEntity"/>
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    async ValueTask<IReadOnlyList<TrackingConfigurationBlame>> IRegexService.GetBlameRegexEditorsAsync(ulong guildId, string name)
    {
        var dbEdits = await _dbContext
            .Set<TrackingDbEntity>()
            .Where(x => x.GuildId == guildId && x.Name == name)
            .Select(x => x.EditorList)
            .FirstAsync();

        var editPairs = dbEdits.Trim(';').Split(';');
        var dict = new List<TrackingConfigurationBlame>();

        foreach (var editPair in editPairs)
        {
            try
            {
                var edit = editPair.Split(':');
                dict.Add(new()
                {
                    // $"{creatorId}:{epoch}:0;"
                    EditorId = ulong.Parse(edit[0]),
                    TimeOfChange = DateTimeOffset.FromUnixTimeSeconds(long.Parse(edit[1])),
                    ChangesMade = (TrackingConfigurationChange)sbyte.Parse(edit[2])
                });
            }
            catch (Exception e)
            {
                dict.Add(new()
                {
                    ErrorReason = e.Message
                });
            }
        }

        return dict;
    }

    bool IRegexService.TrackerDisabled(TrackingDbEntity? tracker)
        => tracker is null
            || tracker.SourceChannelId is 0
            || tracker.ReportChannelId is 0
            || string.IsNullOrWhiteSpace(tracker.RegexPattern);
}

/// <summary>
/// A high level version of <see cref="TrackingDbEntity"/> for delivering and updating values
/// </summary>
public sealed class TrackingConfigurationSummary
{
    public string Name { get; set; } = string.Empty;
    public ulong SourceChannelId { get; set; }
    public ulong ReportChannelId { get; set; }
    public string RegexPattern { get; set; } = string.Empty;

    public TrackingConfigurationChange Changes { get; set; }
}

/// <summary>
/// Shows the change history of a <see cref="TrackingDbEntity"/>
/// </summary>
public sealed class TrackingConfigurationBlame
{
    public DateTimeOffset TimeOfChange { get; init; }
    public ulong EditorId { get; init; }
    public TrackingConfigurationChange ChangesMade { get; init; }

    /// <summary>
    /// Only used when the blame could not be deserialized
    /// </summary>
    public string? ErrorReason { get; init; }
}

/// <summary>
/// Signifies what changes have been made to a <see cref="TrackingConfigurationSummary"/> after
/// calling <see cref="IRegexService.ModifyRegexAsync"/>
/// </summary>
[Flags]
public enum TrackingConfigurationChange : sbyte
{
    None = 0,
    NameChange = 1,
    SourceChannelChange = 2,
    ReportChannelChange = 4,
    RegexPatternChange = 8,
}