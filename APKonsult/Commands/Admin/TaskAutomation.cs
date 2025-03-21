using APKonsult.CommandChecks.Attributes;
using APKonsult.Commands.Admin.TaskRunner;
using APKonsult.Commands.AutoCompleters;
using APKonsult.Context;
using APKonsult.Helpers;
using APKonsult.Models;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace APKonsult.Commands.Admin;

/// <summary>
/// This section uses Lua to do minimal task automation. It could have been worse and been specific implementations that read
/// from JSON files or the SQLite DB. The latter might have been faster, but more of a headache.
/// 
/// Any command that writes to the database will have the RequireBotOwner attribute. If it doesn't interact with the database or
/// is read-only, then it will have RequireAdminUser.
/// </summary>
[Command("action"),
    Description("Task automation configuration."),
    RequireAdminUser]
public class TaskAutomation(APKonsultContext _dbContext)
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
        if (await _dbContext.GetDbGuild(ctx.Guild) is not GuildDbEntity guild)
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

    [Command("set"),
        Description("Creates or sets data elements within an event action."),
        RequireBotOwner]
    public async Task SetHandlerAsync(
        CommandContext ctx,

        [SlashAutoCompleteProvider(typeof(ActionNameAutocomplete)),
            Description(ACTION_NAME_DESCRIPTION)]
        string actionName,

        [SlashAutoCompleteProvider(typeof(EventArgNameAutocomplete)),
            Description(EVENT_NAME_DESCRIPTION)]
        string eventName,

        [Description(SCRIPT_DESCRIPTION),
            RemainingText]
        string script)
    {
        if (await _dbContext.GetDbGuild(ctx.Guild) is not GuildDbEntity guild)
        {
            return;
        }

        script = RemoveScriptBlock(script);

        if (guild.DefinedActions.Find(x => x.ActionName == actionName) is EventAction dbAction)
        {
            dbAction.EventName = eventName;
            dbAction.LuaScript = script;

            await ctx.RespondAsync($"Updated action task `{actionName}` for event `{eventName}`!");
            return;
        }
        else
        {
            guild.DefinedActions.Add(new EventAction()
            {
                ActionName = actionName,
                EventName = eventName,
                LuaScript = script,
                GuildId = guild.Id,
            });
        }

        _ = _dbContext.Guilds.Update(guild);
        _ = await _dbContext.SaveChangesAsync();

        await ctx.RespondAsync($"Saved new action task `{actionName}` for event `{eventName}`!");
    }

    [Command("script"),
        Description("Sets the script of an already existing task action."),
        RequireBotOwner]
    public async Task SetScriptAsync(
        CommandContext ctx,

        [SlashAutoCompleteProvider(typeof(ActionNameAutocomplete)),
            Description(ACTION_NAME_DESCRIPTION)]
        string actionName,

        [Description(SCRIPT_DESCRIPTION)]
        string script)
    {
        if (await _dbContext.GetDbGuild(ctx.Guild) is not GuildDbEntity guild)
        {
            return;
        }

        script = RemoveScriptBlock(script);

        if (guild.GetActionFromName(actionName) is not EventAction action)
        {
            await ctx.RespondAsync($"No event actions are defined with the name `{actionName}`!");
            return;
        }

        action.LuaScript = script;
        _ = await _dbContext.SaveChangesAsync();

        await ctx.RespondAsync($"Updated script for action `{actionName}`!");
    }

    [Command("delete"),
        Description("Deletes a task action from a given action name."),
        RequireBotOwner]
    public async Task DeleteHandlerAsync(
        CommandContext ctx,

        [SlashAutoCompleteProvider(typeof(ActionNameAutocomplete)),
            Description(ACTION_NAME_DESCRIPTION)]
        string actionName)
    {
        if (await _dbContext.GetDbGuild(ctx.Guild) is not GuildDbEntity guild)
        {
            return;
        }

        EventAction? action = guild.DefinedActions.Find(x => x.ActionName == actionName);

        if (action is null)
        {
            await ctx.RespondAsync($"Failed to find any task actions with the name `{actionName}`! No changes made.");
            return;
        }

        _ = guild.DefinedActions.Remove(action);

        _ = _dbContext.Guilds.Update(guild);
        _ = await _dbContext.SaveChangesAsync();

        await ctx.RespondAsync(content: $"Removed task action `{actionName}` successfully!");
    }

    [Command("invokeact"),
        TextAlias("runact"),
        Description("Invoke a set task action without triggering its respective event."),
        RequireBotOwner]
    public async ValueTask InvokeActionTaskAsync(
        CommandContext ctx,

        [SlashAutoCompleteProvider(typeof(ActionNameAutocomplete)),
            Description("The action to invoke.")]
        string actionName)
    {
        if (await _dbContext.GetDbGuild(ctx.Guild) is not GuildDbEntity dbGuild)
        {
            return;
        }

        EventAction? action = dbGuild.DefinedActions.Find(a => a.ActionName == actionName);

        if (action is null)
        {
            await ctx.RespondAsync($"Failed to find any actions by the name `{actionName}`.");
            return;
        }

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle("Lua Action Invoke Results");

        int code;
        long time;

        try
        {
            (code, time) = BotEventLinker.InvokeScript(action, null, ctx.Guild);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync($"Failed to invoke Lua: {e.Message}");
            return;
        }

        _ = embed.WithDescription($"TextScript exited with code: {code}. Execution took {time:n0}ms\nAll other output has been logged.");
        await ctx.RespondAsync(embed);
    }

    [Command("invoke"),
        TextAlias("run"),
        Description("Invoke an arbitrary Lua script without creating a new task action."),
        RequireBotOwner]
    public static async ValueTask InvokeLuaAsync(
        CommandContext ctx,

        [Description(SCRIPT_DESCRIPTION), RemainingText]
        string script)
    {
        script = RemoveScriptBlock(script);

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle("Lua Invoke Results");

        int code;
        long time;

        try
        {
            (code, time) = BotEventLinker.InvokeScript(new EventAction()
            {
                ActionName = "Direct-Invoke-A",
                EventName = "Direct-Invoke-E",
                LuaScript = script,
            }, null, ctx.Guild);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync($"Failed to invoke Lua: {e.Message}");
            return;
        }

        _ = embed.WithDescription($"TextScript exited with code: {code}. Execution took {time:n0}ms\nAll other output has been logged.");
        await ctx.RespondAsync(embed);
    }

    [Command("deploy"),
        Description("Moves database task actions into the action cache to be used."),
        RequireAdminUser]
    public async Task DeployHandlers(
        CommandContext ctx,

        [SlashAutoCompleteProvider(typeof(ActionNameAutocomplete)),
            Description(ACTION_NAME_DESCRIPTION)]
        string actionNamesList)
    {
        GuildDbEntity? guild = await _dbContext.GetDbGuild(ctx.Guild);
        if (guild is null)
        {
            return;
        }

        var actionNames = actionNamesList.Split(',');

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle($"Deploying {actionNames.Length} Task Action{"s".Pluralize(actionNames.Length)}");

        foreach (string actionName in actionNames.Select(s => s.Trim()))
        {
            EventAction? action = guild.DefinedActions.Find(a => a.ActionName == actionName);

            if (action is null)
            {
                _ = embed.AddField("Error", $"Failed to find action task '{actionName}'");
                continue;
            }

            string status = BotEventLinker.DeployTaskAction(ctx.Guild!, action);
            (int code, long initMs) = BotEventLinker.InvokeScript(action, null, ctx.Guild);
            _ = embed.AddField(status, $"{actionName} - `{action.EventName}` ({GBConverter.FormatSizeFromBytes(action.LuaScript.Length)})\n" +
                $"Init returned {code} and took {initMs}ms.");

            if (code is 0)
            {
                action.Enabled = true;
            }
        }

        await ctx.RespondAsync(embed);
    }

    [Command("disable"),
        Description("Uninstalls/kills a running Lua task action."),
        RequireBotOwner]
    public async Task DisableHandlers(
        CommandContext ctx,

        [SlashAutoCompleteProvider(typeof(ActionNameAutocomplete)),
            Description(ACTION_NAME_DESCRIPTION)]
        string actionNamesList)
    {
        GuildDbEntity? guild = await _dbContext.GetDbGuild(ctx.Guild);
        if (guild is null)
        {
            return;
        }

        var actionNames = actionNamesList.Split(',');

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle($"Disabling {actionNames.Length} Task Action{"s".Pluralize(actionNames.Length)}");

        foreach (string actionName in actionNames.Select(s => s.Trim()))
        {
            EventAction? action = guild.DefinedActions.Find(a => a.ActionName == actionName);

            if (action is null)
            {
                _ = embed.AddField("Error", $"Failed to find action task '{actionName}'");
                continue;
            }

            string status = BotEventLinker.DisableTaskAction(action);
            _ = embed.AddField(status, $"{actionName} - `{action.EventName}`");
        }

        await ctx.RespondAsync(embed);
    }

    [Command("active"),
        Description("Shows which task actions are enabled in the task action cache."),
        RequireAdminUser]
    public static async Task ShowActiveHandlers(
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
            .WithColor();

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
            else if (action.Enabled)
            {
                status = "Deployed";
            }
            else
            {
                status = "Disabled";
            }

            embed.AddField($"{action.ActionName} (`{action.EventName}`)", status);
        }
    }

    public class BotEventLinker : IEventHandler<DiscordEventArgs>
    {
        /// <summary>
        /// Holds all of a guilds actions (refreshed by using 'action deploy', or restarting bot)
        /// </summary>
        internal static readonly List<GuildActionInfo> GuildActionCache = [];

        private readonly APKonsultContext _dbContext;
        private readonly Dictionary<Type, PropertyInfo> guildPropertyMap = [];

        public BotEventLinker(APKonsultContext _db)
        {
            _dbContext = _db;
        }

        public async Task HandleEventAsync(DiscordClient sender, DiscordEventArgs eventArgs)
        {
            DiscordGuild? guild = GetGuildFromType(eventArgs);
            if (guild is null)
            {
                return;
            }

            GuildActionInfo? actions =
                // Check cache
                CheckActionCache(guild)
                // Check DB
                ?? await CheckDbGuild(guild);

            if (actions is null)
            {
                return;
            }

            IEnumerable<EventAction> foundActions = actions.Scripts.Where(a => a.Enabled && a.EventName == eventArgs.GetType().Name);

            if (!foundActions.Any())
            {
                return;
            }

            foreach (EventAction action in foundActions)
            {
                _ = InvokeScript(action, eventArgs, guild);
            }
        }

        public static (int exitCode, long executionTimeMs) InvokeScript(EventAction action, DiscordEventArgs? args, DiscordGuild guild)
        {
            Stopwatch luaWatch = Stopwatch.StartNew();

            TaskRuntime runtime = TryGetCachedRuntime(action);
            int result;

            if (runtime.Active && args is not null)
            {
                result = runtime.VisitCallback(args);
            }
            else
            {
                result = runtime.ExecuteScript(action, args);

                if (runtime.Active)
                {
                    CacheRuntime(runtime, guild.Id);
                }
            }

            luaWatch.Stop();

            if (result is not 0)
            {
                Log.Error("Lua task '{ActionName}' exited with exit code {Result}", action.ActionName, result);
            }

            Log.Debug("Lua took {ElapsedMilliseconds:n0}ms", luaWatch.ElapsedMilliseconds);

            return (result, luaWatch.ElapsedMilliseconds);
        }

        public static string DeployTaskAction(DiscordGuild guild, EventAction action)
        {
            const string INSTALLED = "Installed",
                         UPDATED = "Updated";

            GuildActionInfo? cachedActions = CheckActionCache(guild);

            if (cachedActions is null)
            {
                // cachedActions = new()
                // {
                //     GuildId = guild.Id,
                //     Scripts = [action]
                // };
                // 
                // GuildActionCache.Add(cachedActions);
                // return INSTALLED;

                throw new Exception("what");
            }

            EventAction? deployedAction = cachedActions.Scripts.Find(a => a.ActionName == action.ActionName);

            if (deployedAction is null)
            {
                cachedActions.Scripts.Add(action);
                return INSTALLED;
            }

            deployedAction.LuaScript = action.LuaScript;
            return UPDATED;
        }

        public static string DisableTaskAction(EventAction action)
        {
            var guildInfo = GuildActionCache
                .First(g => g.GuildId == action.GuildId);

            string result = "Removed from cache";

            var runtime = guildInfo.ActiveRuntimes.Find(r => r.Action.ActionName == action.ActionName);
            if (runtime is not null)
            {
                guildInfo.ActiveRuntimes.Remove(runtime);
                result = $"{result}, LuaRuntime killed";
            }

            guildInfo.Scripts
                .First(s => s.ActionName == action.ActionName)
                .Enabled = false;

            return result;
        }

        private static void CacheRuntime(TaskRuntime runtime, ulong guildId)
        {
            GuildActionInfo guildInfo = GetGuildInfo(guildId);
            guildInfo.ActiveRuntimes.Add(runtime);
        }

        private static TaskRuntime TryGetCachedRuntime(EventAction action)
        {
            GuildActionInfo? guildInfo = GuildActionCache.Find(g => g.GuildId == action.GuildId);

            if (guildInfo is null)
            {
                // guildInfo = new()
                // {
                //     GuildId = action.GuildId,
                //     Scripts = [action]
                // };
                // 
                // GuildActionCache.Add(guildInfo);
                return new TaskRuntime(action.LuaScript);
            }

            TaskRuntime? cachedRuntime = guildInfo.ActiveRuntimes.Find(a => a.Action?.ActionName == action.ActionName);

            if (cachedRuntime is null)
            {
                return new TaskRuntime(action.LuaScript);
            }

            return cachedRuntime;
        }

        private static GuildActionInfo? CheckActionCache(DiscordGuild guild)
        {
            return GuildActionCache.Find(action => action.GuildId == guild.Id);
        }

        private async ValueTask<GuildActionInfo?> CheckDbGuild(DiscordGuild guild)
        {
            if (guild is null)
            {
                return null;
            }

            GuildDbEntity? dbGuild = await _dbContext.GetDbGuild(guild);
            if (dbGuild is null
                || dbGuild.DefinedActions is null
                || dbGuild.DefinedActions.Count == 0)
            {
                return null;
            }

            GuildActionInfo gAction = new()
            {
                GuildId = guild.Id,
                Scripts = [.. dbGuild.DefinedActions]
            };

            // Add to cache
            GuildActionCache.Add(gAction);

            // Preload all actions
            foreach (var action in dbGuild.DefinedActions)
            {
                Log.Information("Initializing action {ActionName} for guild {GuildId}", action.ActionName, action.GuildId);
                InvokeScript(action, null, guild);
            }

            return gAction;
        }

        private DiscordGuild? GetGuildFromType(DiscordEventArgs eventArgs)
        {
            Type argType = eventArgs.GetType();

            if (!guildPropertyMap.TryGetValue(argType, out PropertyInfo? guildProperty))
            {
                guildProperty = argType.GetProperty("Guild", BindingFlags.Instance | BindingFlags.Public);

                if (guildProperty is null)
                {
                    return null;
                }

                guildPropertyMap[argType] = guildProperty;
            }

            return guildProperty is not null
                && guildProperty.PropertyType == typeof(DiscordGuild)
                && guildProperty.GetValue(eventArgs) is DiscordGuild guild
                    ? guild
                    : null;
        }

        private static GuildActionInfo GetGuildInfo(ulong guildId)
        {
            GuildActionInfo? guildInfo = GuildActionCache.Find(g => g.GuildId == guildId);

            if (guildInfo is null)
            {
                guildInfo = new GuildActionInfo()
                {
                    GuildId = guildId
                };

                GuildActionCache.Add(guildInfo);
            }

            return guildInfo;
        }

        internal sealed class GuildActionInfo
        {
            public required ulong GuildId { get; init; }
            public List<EventAction> Scripts { get; set; } = [];
            public List<TaskRuntime> ActiveRuntimes { get; set; } = [];
        }
    }

    private static string RemoveScriptBlock(string script)
    {
        return Shared.TryRemoveCodeBlock(script, CodeType.All, out string? parsedScript) ? parsedScript : script;
    }
}

internal static class EventTaskExtensions
{
    public static async Task<GuildDbEntity?> GetDbGuild(this APKonsultContext db, DiscordGuild? guild)
    {
        if (guild is null)
        {
            return null;
        }

        return await db.Guilds
            .Include(x => x.DefinedActions)
            .FirstOrDefaultAsync(x => x.Id == guild.Id) is GuildDbEntity dbGuild
                ? dbGuild
                : null;
    }

    public static EventAction? GetActionFromName(this GuildDbEntity? guild, string actionName)
    {
        return guild?.DefinedActions.Find(x => x.ActionName == actionName);
    }
}