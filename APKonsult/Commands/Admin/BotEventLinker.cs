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
            ?? await CheckDbGuildAsync(guild);

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
            _ = await InvokeScriptAsync(action, eventArgs, guild);
        }
    }

    public static async ValueTask<(int exitCode, long executionTimeMs, Exception? exception)> InvokeScriptAsync(EventAction action, DiscordEventArgs? args, DiscordGuild guild)
    {
        Stopwatch luaWatch = Stopwatch.StartNew();

        TaskRuntime runtime = TryGetCachedRuntime(action);
        int result;

        try
        {
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
        }
        catch (Exception ex)
        {
            luaWatch.Stop();

            Log.Error(ex, "Lua task '{ActionName}' failed for guild ({Name}) {GuildId}", action.ActionName, guild.Name, action.GuildId);
            await ex.LogToWebhookAsync();

            return (-1, luaWatch.ElapsedMilliseconds, null);
        }

        luaWatch.Stop();

        if (result is not 0)
        {
            Log.Error("Lua task '{ActionName}' exited with exit code {Result}", action.ActionName, result);
        }

        Log.Debug("Lua took {ElapsedMilliseconds:n0}ms", luaWatch.ElapsedMilliseconds);

        return (result, luaWatch.ElapsedMilliseconds, null);
    }

    public static string DeployTaskAction(DiscordGuild guild, EventAction action)
    {
        const string INSTALLED = "Installed",
                     UPDATED = "Updated",
                     KILLED = "Killed";

        GuildActionInfo? cachedActions = CheckActionCache(guild);

        if (cachedActions is null)
        {
            return "$NoGuildCache";
        }

        // Remove from action cache

        int deployedActionIndex = cachedActions.Scripts.FindIndex(a => a.ActionName == action.ActionName);

        if (deployedActionIndex is -1)
        {
            cachedActions.Scripts.Add(action);
            return INSTALLED;
        }

        // Renew the action cache, kill any old task runtimes

        string status = UPDATED;

        cachedActions.Scripts.RemoveAt(deployedActionIndex);
        cachedActions.Scripts.Add(action);

        int runningTaskIndex = cachedActions.ActiveRuntimes.FindIndex(r => r.Action.ActionName == action.ActionName);

        if (runningTaskIndex is not -1)
        {
            cachedActions.ActiveRuntimes.RemoveAt(runningTaskIndex);
            status = $"{KILLED}, {UPDATED}";
        }

        return status;
    }

    public static string KillTaskAction(EventAction action)
    {
        GuildActionInfo guildInfo = GuildActionCache
            .First(g => g.GuildId == action.GuildId);

        string result = "Removed from cache";

        TaskRuntime? runtime = guildInfo.ActiveRuntimes.Find(r => r.Action.ActionName == action.ActionName);
        if (runtime is not null)
        {
            _ = guildInfo.ActiveRuntimes.Remove(runtime);
            result = $"{result}, LuaRuntime killed";
        }

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
            return new TaskRuntime(action.LuaScript);
        }

        TaskRuntime? cachedRuntime = guildInfo.ActiveRuntimes.Find(a => a.Action?.ActionName == action.ActionName);

        return cachedRuntime is null
            ? new TaskRuntime(action.LuaScript)
            : cachedRuntime;
    }

    private static GuildActionInfo? CheckActionCache(DiscordGuild guild)
    {
        return GuildActionCache.Find(action => action.GuildId == guild.Id);
    }

    private async ValueTask<GuildActionInfo?> CheckDbGuildAsync(DiscordGuild guild)
    {
        if (guild is null)
        {
            return null;
        }

        GuildDbEntity? dbGuild = await _dbContext.GetDbGuildAsync(guild);
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

        // Preload all enabled actions
        foreach (EventAction? action in dbGuild.DefinedActions.Where(a => a.Enabled))
        {
            Log.Information("Initializing action {ActionName} for guild {GuildId}", action.ActionName, action.GuildId);
            _ = InvokeScriptAsync(action, null, guild);
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