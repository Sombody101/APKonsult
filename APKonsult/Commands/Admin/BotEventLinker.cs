using APKonsult.Commands.Admin.TaskRunner;
using APKonsult.Context;
using APKonsult.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Serilog;
using System.Diagnostics;
using System.Reflection;

namespace APKonsult.Commands.Admin;

public class BotEventLinker(APKonsultContext Db) : IEventHandler<DiscordEventArgs>
{
    /// <summary>
    /// Holds all of a guilds actions (refreshed by using 'action deploy', or restarting bot)
    /// </summary>
    internal static readonly List<GuildActionInfo> GuildActionCache = [];

    private readonly APKonsultContext _dbContext = Db;
    private readonly Dictionary<Type, PropertyInfo> guildPropertyMap = [];

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

        string eventName = eventArgs.GetType().Name;
        IEnumerable<EventAction> foundActions = actions.Scripts.Where(a => a.Enabled && a.EventName == eventName);

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
            return "$NoGuildCache";
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
        foreach (var action in dbGuild.DefinedActions.Where(a => a.Enabled))
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