using APKonsult.Commands.AutoCompleters;
using APKonsult.Models;
using APKonsult.Services.RegexServices;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace APKonsult.Commands.Moderation;

[Command("tracker"), Description("Module for tracking guild messages in specific channels.")]
[RequirePermissions(DiscordPermission.ModerateMembers), RequireGuild]
public class ContentTrackerCommand
{
    private const string configName = "config_name";
    private const string targetId = "target_channel";
    private const string reportId = "report_channel";
    private const string regex = "regex";

    private readonly IRegexService _regexService;
    private readonly InteractivityExtension _interactivity;

    public ContentTrackerCommand(IRegexService _regex, InteractivityExtension interactivity)
    {
        _regexService = _regex;
        _interactivity = interactivity;
    }

    /// <summary>
    /// Adds a new regex tracker to the <see cref="RegexService"/>
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    [Command("add"), Description("Creates a new regex tracker for a channel.")]
    public async ValueTask AddTrackerAsync(SlashCommandContext ctx)
    {
        TrackingConfigurationSummary? result = await PromptWithTrackerModalAsync(ctx);

        if (result is null)
        {
            return;
        }

        try
        {
            await _regexService.CreateRegexAsync(ctx.Guild.Id, ctx.User.Id, result);
        }
        catch (Exception e)
        {
            _ = await ctx.FollowupAsync(new DiscordEmbedBuilder()
                .WithTitle("Unable to create tracker!")
                .WithColor(DiscordColor.Red)
                .WithDescription(e.Message));
        }

        // var tracker = _regexService.get

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle("Regex tracking pattern created!")
            .AddField("Name", result.Name);

        _ = result.SourceChannelId is not 0
            ? embed.AddField("Source Channel", $"{result.SourceChannelId} ({ctx.Guild.GetChannelAsync(result.SourceChannelId).Result.Mention})")
            : embed.AddField("Source Channel", "Unset (Tracker disabled)");

        _ = result.ReportChannelId is not 0
            ? embed.AddField("Report Channel", $"{result.ReportChannelId} ({ctx.Guild.GetChannelAsync(result.ReportChannelId).Result.Mention})")
            : embed.AddField("Report Channel", "Unset (Tracker disabled)");

        _ = embed.AddField("Regex Patthen", $"```regex\n{result.RegexPattern}\n```");

        _ = await ctx.FollowupAsync(embed);
    }

    /// <summary>
    /// Remove a <see cref="TrackingDbEntity"/> from a <see cref="DiscordGuild"/>
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="tracker_name"></param>
    /// <returns></returns>
    [Command("delete"), Description("Deletes the specified tracker configuration")]
    public async ValueTask DeleteTrackerAsync(CommandContext ctx,
        [RemainingText]
        [SlashAutoCompleteProvider(typeof(TrackerNameAutocomplete))]
        string tracker_name)
    {
        await _regexService.DeleteRegexAsync(ctx.Guild.Id, tracker_name);

        await ctx.RespondAsync($"Deleted tracker `{tracker_name}`!");
    }

    /// <summary>
    /// Get information on a specific <see cref="TrackingDbEntity"/>
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="tracker_name"></param>
    /// <returns></returns>
    [Command("get"), Description("Gets the configuration for a channel regex tracker."), DefaultGroupCommand]
    public async ValueTask GetTrackerAsync(CommandContext ctx,
        [RemainingText]
        [SlashAutoCompleteProvider(typeof(TrackerNameAutocomplete))]
        string tracker_name)
    {
        TrackingDbEntity? tracker = await _regexService.GetRegexAsync(ctx.Guild.Id, tracker_name);

        if (tracker is null)
        {
            await ctx.RespondAsync($"Failed to find a tracker by the name '{tracker_name}'");
            return;
        }

        await ctx.RespondAsync(new DiscordEmbedBuilder()
            .WithTitle("Regex Tracker Info")
            .WithDescription($"### {tracker.Name}\nTracker source channel: `{tracker.SourceChannelId}`\nTracker reporter channel: `{tracker.ReportChannelId}`\n" +
            $"Tracker regex expression\n```log\n{tracker.RegexPattern}\n```"));
    }

    /// <summary>
    /// Get every <see cref="TrackingDbEntity"/> associated with a <see cref="DiscordGuild"/>
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    [Command("list"), Description("Lists all regex trackers for this guild.")]
    public async ValueTask ListTrackersAsync(CommandContext ctx)
    {
        IReadOnlyCollection<TrackingDbEntity>? trackers = await _regexService.GetRegexesOwnedByGuildAsync(ctx.Guild.Id);

        if (trackers is null || trackers.Count is 0)
        {
            await ctx.RespondAsync("This guild doesn't have any regex trackers set!");
            return;
        }

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle("Set Regex Trackers");

        foreach (TrackingDbEntity tracker in trackers)
        {
            _ = embed.AddField(tracker.Name,
                $"Source channel: `{tracker.SourceChannelId}`\nReporter channel: `{tracker.ReportChannelId}`\nRegex expression: ```regex\n{tracker.RegexPattern}\n```");
        }

        await ctx.RespondAsync(embed);
    }

    /// <summary>
    /// Get the edit history of a specific guild <see cref="TrackingDbEntity"/>
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="tracker_name"></param>
    /// <returns></returns>
    [Command("blame"), Description("Gets the edit history of a tracker.")]
    public async ValueTask GetEditorsAsync(CommandContext ctx,
        [RemainingText]
        [SlashAutoCompleteProvider(typeof(TrackerNameAutocomplete))]
        string tracker_name)
    {
        IReadOnlyList<TrackingConfigurationBlame> editBlames = await _regexService.GetBlameRegexEditorsAsync(ctx.Guild.Id, tracker_name);

        StringBuilder formattedEditors = new();

        foreach (TrackingConfigurationBlame blame in editBlames)
        {
            if (blame.ErrorReason is not null)
            {
                _ = formattedEditors.AppendLine($"1. Failed to fetch edit details!\n - {blame.ErrorReason}");
                continue;
            }

            long epoch = blame.TimeOfChange.ToUnixTimeSeconds();
            _ = formattedEditors.AppendLine($"1. `{blame.EditorId}` ({ctx.Client.GetUserAsync(blame.EditorId).Result.Username})")
                .Append(FormatChanges(blame.ChangesMade))
                .AppendLine($" - Change made <t:{epoch}:F>, or <t:{epoch}:R>");
        }

        bool isActive = _regexService.TrackerDisabled(await _regexService.GetRegexAsync(ctx.Guild.Id, tracker_name));

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle($"Tracker Editor History [{(isActive ? "ACTIVE" : "INACTIVE")}]")
            .WithDescription(formattedEditors.ToString());

        await ctx.RespondAsync(embed);
    }

    [Command("edit")]
    public class TrackerModifications
    {
        private readonly IRegexService _regexService;

        public TrackerModifications(IRegexService _regex)
        {
            _regexService = _regex;
        }

        /// <summary>
        /// Change one or more properties of a <see cref="TrackingDbEntity"/> via a modal
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="tracker_name"></param>
        /// <returns></returns>
        [Command("edit"), Description("Edit a regex tracker")]
        public async ValueTask EditTrackerAsync(SlashCommandContext ctx,
            [RemainingText]
            [SlashAutoCompleteProvider(typeof(TrackerNameAutocomplete))]
            string tracker_name)
        {
            TrackingDbEntity? tracker = await _regexService.GetRegexAsync(ctx.Guild.Id, tracker_name);

            if (tracker is null)
            {
                await ctx.RespondAsync($"Failed to find a tracker by the name `{tracker_name}`");
                return;
            }

            TrackingConfigurationSummary? result = null;// await PromptWithTrackerModalAsync(ctx, tracker);

            if (result is null)
            {
                return;
            }

            StringBuilder changedItems = new();

            if (result.Name != tracker.Name)
            {
                _ = changedItems.AppendLine($"1. Tracker name changed from `{tracker.Name}` to `{result.Name}`");
            }

            if (result.SourceChannelId != tracker.SourceChannelId)
            {
                _ = changedItems.AppendLine($"1. Source channel changed from `{tracker.SourceChannelId}` to `{result.SourceChannelId}`");
            }

            if (result.ReportChannelId != tracker.ReportChannelId)
            {
                _ = changedItems.AppendLine($"1. Reporter channel changed from `{tracker.ReportChannelId}` to `{result.ReportChannelId}`");
            }

            if (result.RegexPattern != tracker.RegexPattern)
            {
                _ = changedItems.AppendLine($"1. Regex pattern changed from\n```regex\n{tracker.RegexPattern}\n``` to\n```regex\n{result.RegexPattern}\n```");
            }

            try
            {
                await _regexService.ModifyRegexAsync(ctx.Guild.Id, ctx.User.Id, tracker_name, result);
            }
            catch (Exception e)
            {
                _ = await ctx.FollowupAsync(new DiscordEmbedBuilder()
                    .WithTitle("Unable to edit tracker!")
                    .WithColor(DiscordColor.Red)
                    .WithDescription(e.Message));
            }

            _ = await ctx.FollowupAsync($"Updated tracker!\n{changedItems}");
        }
    }

    private async ValueTask<TrackingConfigurationSummary?> PromptWithTrackerModalAsync(SlashCommandContext ctx, TrackingDbEntity? modifying = null)
    {
        string modalName = Shared.GenerateModalId();
        bool isModifying = modifying is not null;

        DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder()
            .WithCustomId(modalName)
            .WithTitle("Channel Tracking Configuration")

            // Config name
            .AddTextInputComponent(new DiscordTextInputComponent("Configuration Name", configName, "My Config", isModifying
                ? modifying.Name
                : null, max_length: 70))

            // Source channel ID
            .AddTextInputComponent(new DiscordTextInputComponent("Look for messages in:", targetId, "Channel ID (0 disables tracker)", isModifying
                ? modifying.SourceChannelId.ToString()
                : null, max_length: 20))

            // Reporter channel ID
            .AddTextInputComponent(new DiscordTextInputComponent("Report messages in:", reportId, "Channel ID (0 disables tracker)", isModifying
                ? modifying.ReportChannelId.ToString()
                : null, max_length: 20))

            // Regex string
            .AddTextInputComponent(new DiscordTextInputComponent("Regex String", regex, "^([a-zA-Z0-9_\\-\\.]+)@([a-zA-Z0-9_\\-\\.]+)$", isModifying
                ? modifying.RegexPattern
                : null, max_length: 512, required: false));

        await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);

        InteractivityResult<DSharpPlus.EventArgs.ModalSubmittedEventArgs> response = await _interactivity.WaitForModalAsync(modalName);

        if (response.TimedOut)
        {
            _ = await ctx.FollowupAsync("The tracking configuration builder timed out!");
            return null;
        }

        StringBuilder errors = new();

        // Validate channels
        if (!ulong.TryParse(response.Result.Values[targetId], out ulong target_id))
        {
            _ = errors.AppendLine("The target channel ID must be a number!");
        }

        if (target_id is not 0)
        {
            DiscordChannel? channel = await ctx.Guild.GetChannelAsync(target_id);
            if (channel is null)
            {
                _ = errors.AppendLine($"Failed to find a target channel with the given ID `{target_id}`!");
            }
        }

        if (!ulong.TryParse(response.Result.Values[reportId], out ulong report_id))
        {
            _ = errors.AppendLine("The report channel ID must be a number!");
        }

        if (report_id is not 0)
        {
            DiscordChannel? channel = await ctx.Guild.GetChannelAsync(report_id);
            if (channel is null)
            {
                _ = errors.AppendLine($"Failed to find a report channel with the given ID `{report_id}`!");
            }
        }

        // Test the regex
        string pattern = response.Result.Values[regex];
        try
        {
            _ = Regex.Match(string.Empty, pattern);
        }
        catch (ArgumentException ex)
        {
            _ = errors.AppendLine("The given regex expression does not work!")
                .Append($" - {ex.Message}");
        }

        // Report errors, return if any
        if (errors.Length is not 0)
        {
            _ = await ctx.FollowupAsync(new DiscordEmbedBuilder()
                .WithTitle("An error occured while creating your configuration!")
                .WithDescription(errors.ToString()));

            return null;
        }

        // Return organized data
        return new()
        {
            Name = response.Result.Values[configName],
            SourceChannelId = target_id,
            ReportChannelId = report_id,
            RegexPattern = pattern,
        };
    }

    private static string FormatChanges(TrackingConfigurationChange change)
    {
        StringBuilder changeBuilder = new();

        if (change.HasFlag(TrackingConfigurationChange.NameChange))
        {
            _ = changeBuilder.AppendLine(" - Changed config name.");
        }

        if (change.HasFlag(TrackingConfigurationChange.SourceChannelChange))
        {
            _ = changeBuilder.AppendLine(" - Changed source channel ID.");
        }

        if (change.HasFlag(TrackingConfigurationChange.ReportChannelChange))
        {
            _ = changeBuilder.AppendLine(" - Changed report channel ID.");
        }

        if (change.HasFlag(TrackingConfigurationChange.RegexPatternChange))
        {
            _ = changeBuilder.AppendLine(" - Changed regex pattern.");
        }

        if (change == 0)
        {
            _ = changeBuilder.AppendLine(" - Tracker created.");
        }

        return changeBuilder.ToString();
    }
}
