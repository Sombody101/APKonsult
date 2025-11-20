using APKonsult.CommandChecks.Attributes;
using APKonsult.Commands.AutoCompleters;
using APKonsult.Context;
using APKonsult.Helpers;
using APKonsult.Models.Main;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace APKonsult.Commands.Admin;

/// <summary>
/// This section uses Lua to do minimal task automation. It could have been worse and been specific implementations that read
/// from JSON files or the SQLite DB. The latter might have been faster, but more of a headache when something should be changed.
/// 
/// Any command that writes to the database will have the <see cref="RequireBotOwnerAttribute"/> attribute. If it doesn't interact with the database or
/// is read-only, then it will have <see cref="RequireAdminUserAttribute"/>.
/// </summary>
[Command("action"),
    TextAlias("actions"),
    Description("Task automation configuration."),
    RequireAdminUser]
public sealed partial class TaskAutomation(APKonsultContext _dbContext)
{
    private const string ACTION_NAME_DESCRIPTION = "The name for the wanted task action.";
    private const string EVENT_NAME_DESCRIPTION = "The name for the wanted task event.";
    private const string SCRIPT_DESCRIPTION = "The script to be added or set on a task action.";

    [Command("list"),
        TextAlias("info"),
        Description("Lists all defined event actions, or data on a specific one."),
        RequireAdminUser]
    public async Task ListActionsAsync(
        CommandContext ctx,

        [SlashAutoCompleteProvider(typeof(ActionNameAutocomplete)),
            Description(ACTION_NAME_DESCRIPTION)]
        string actionName = null!)
    {
        if (await _dbContext.GetDbGuildAsync(ctx.Guild) is not GuildDbEntity guild)
        {
            return;
        }

        if (guild.DefinedActions.Count is 0)
        {
            await ctx.RespondAsync("There are no defined event actions!");
            return;
        }

        if (actionName is not null)
        {
            EventAction? action = guild.DefinedActions.Find(a => a.ActionName == actionName);

            if (action is null)
            {
                await ctx.RespondAsync($"No action by the name '{actionName}' exists!");
                return;
            }

            DiscordEmbedBuilder actionEmbed = new DiscordEmbedBuilder()
                .WithTitle("Action Information")
                .AddField("Name", action.ActionName)
                .AddField("Event", action.EventName)
                .WithDescription($"```lua\n{action.LuaScript}\n```")
                .MakeWide();

            await ctx.RespondAsync(actionEmbed);
            return;
        }

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle("Defined Event Actions");

        foreach (EventAction action in guild.DefinedActions)
        {
            _ = embed.AddField($"*{action.ActionName}*", $"`{action.EventName}` ({GBConverter.FormatSizeFromBytes(action.LuaScript.Length)})");
        }

        await ctx.RespondAsync(embed);
    }

    [Command("active"),
        Description("Shows which task actions are enabled in the task action cache."),
        RequireAdminUser]
    public static async Task ShowActiveHandlersAsync(
        CommandContext ctx,

        [SlashAutoCompleteProvider(typeof(ActionNameAutocomplete)),
            Description(ACTION_NAME_DESCRIPTION)]
        string? actionName = null)
    {
        BotEventLinker.GuildActionInfo? guild = BotEventLinker.GuildActionCache
                .Find(g => g.GuildId == ctx.Guild.Id);

        if (guild is null)
        {
            await ctx.RespondAsync("This guild has no active actions!");
            return;
        }

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle("Task Action Status")
            .WithFooter("Deployed: In memory awaiting invocation.\nRunning: A LuaRuntime for this action exists and its event handler is pre-loaded.")
            .WithDefaultColor();

        if (actionName is not null)
        {
            EventAction? action = guild.Scripts.Find(a => a.ActionName == actionName);

            if (action is null)
            {
                await ctx.RespondAsync($"There is no active action '{actionName}'");
                return;
            }

            AddActionStatus(embed, action);
        }
        else
        {
            foreach (EventAction action in guild.Scripts)
            {
                AddActionStatus(embed, action);
            }
        }

        await ctx.RespondAsync(embed);

        void AddActionStatus(DiscordEmbedBuilder embed, EventAction action)
        {
            string status;

            if (guild.ActiveRuntimes.Exists(r => r.Action.ActionName == action.ActionName))
            {
                status = "Running";
            }
            else
            {
                status = action.Enabled
                    ? "Deployed"
                    : "Disabled";
            }

            _ = embed.AddField($"{action.ActionName} (`{action.EventName}`)", status);
        }
    }

    private static string RemoveScriptBlock(string script)
    {
        return Shared.TryRemoveCodeBlock(script, CodeType.All, out string? parsedScript)
            ? parsedScript
            : script;
    }
}

internal static class EventTaskExtensions
{
    public static async Task<GuildDbEntity?> GetDbGuildAsync(this APKonsultContext db, DiscordGuild? guild)
    {
        return guild is null 
            ? null 
            : await GetDbGuildAsync(db, guild.Id);
    }

    public static async Task<GuildDbEntity?> GetDbGuildAsync(this APKonsultContext db, ulong guildId)
    {
        return await db.Guilds
            .Include(x => x.DefinedActions)
            .FirstOrDefaultAsync(x => x.Id == guildId);
    }

    public static EventAction? GetActionFromName(this GuildDbEntity? guild, string actionName)
    {
        return guild?.DefinedActions.Find(x => x.ActionName == actionName);
    }
}