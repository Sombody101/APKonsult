using APKonsult.CommandChecks.Attributes;
using APKonsult.Commands.AutoCompleters;
using APKonsult.Helpers;
using APKonsult.Models;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using System.ComponentModel;
using System.Text;

namespace APKonsult.Commands.Admin;

public partial class TaskAutomation
{
    [Command("deploy"),
        TextAlias("enable"),
        Description("Moves database task actions into the action cache to be used."),
        RequireAdminUser]
    public async Task DeployHandlersAsync(
        CommandContext ctx,

        [SlashAutoCompleteProvider(typeof(ActionNameAutocomplete)),
            Description(ACTION_NAME_DESCRIPTION)]
        string actionNamesList = "all")
    {
        GuildDbEntity? guild = await _dbContext.GetDbGuildAsync(ctx.Guild);
        if (guild is null)
        {
            return;
        }

        List<EventAction> actionsToDeploy = [];

        DiscordEmbedBuilder embed = new();

        if (actionNamesList.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            actionsToDeploy = guild.DefinedActions;
        }
        else
        {
            string[] l = actionNamesList.Split(',');

            foreach (string name in l)
            {
                EventAction? action = guild.DefinedActions.Find(a => a.ActionName == name);

                if (action is null)
                {
                    _ = embed.AddField("Error", $"Failed to find action task '{name}'");
                    continue;
                }

                actionsToDeploy.Add(action);
            }
        }

        _ = embed.WithTitle($"Deploying {actionsToDeploy.Count} Task {"Action".Pluralize(actionsToDeploy.Count)}");

        foreach (EventAction action in actionsToDeploy)
        {
            string status = BotEventLinker.DeployTaskAction(ctx.Guild!, action);

            (int code, long initMs, Exception? exception) = await BotEventLinker.InvokeScriptAsync(action, null, ctx.Guild);

            StringBuilder sb = new();
            sb.Append(action.ActionName).Append(" - `").Append(action.EventName).Append("` (").Append(GBConverter.FormatSizeFromBytes(action.LuaScript.Length)).AppendLine(")")
                .Append("Init returned ").Append(code).Append(" and took ").Append(initMs).AppendLine("ms.");

            if (exception is not null)
            {
                sb.Append("Exception: ").AppendLine(exception.Message);
            }

            _ = embed.AddField(status, sb.ToString());

            if (code is 0)
            {
                action.Enabled = true;
            }
        }

        _ = await _dbContext.SaveChangesAsync();
        await ctx.RespondAsync(embed.WithDefaultColor());
    }

    // This one requires the bot owner only because purging could be disruptive
    [Command("purge"),
        Description("Clears all instances of a task action for all caches."),
        RequireBotOwner]
    public async ValueTask PurgeActionsAsync(
        CommandContext ctx,

        [SlashAutoCompleteProvider(typeof(ActionNameAutocomplete)),
            Description(ACTION_NAME_DESCRIPTION)]
        string actionName)
    {
        var actionCache = BotEventLinker.GuildActionCache.Find(g => g.GuildId == ctx.Guild.Id);

        if (actionCache is null)
        {
            return;
        }

        string result = string.Empty;
        EventAction foundAction = null!;

        int cachedScriptIndex = actionCache.Scripts.FindIndex(s => s.ActionName == actionName);
        if (cachedScriptIndex is not -1)
        {
            foundAction = actionCache.Scripts[cachedScriptIndex];
            actionCache.Scripts.RemoveAt(cachedScriptIndex);
            result = "Removed from cache";
        }

        int cachedRuntimeIndex = actionCache.ActiveRuntimes.FindIndex(r => r.Action.ActionName == actionName);
        if (cachedRuntimeIndex is not -1)
        {
            foundAction = actionCache.ActiveRuntimes[cachedScriptIndex].Action;
            actionCache.ActiveRuntimes.RemoveAt(cachedRuntimeIndex);
            result = $"{result}, runtime killed";
        }

        if (result == string.Empty)
        {
            await ctx.RespondAsync($"No action '{actionName}' was found in the action cache!");
            return;
        }

        await ctx.RespondAsync(new DiscordEmbedBuilder()
            .WithTitle($"{actionName} (`{foundAction.EventName}`)")
            .AddField("Status", result)
            .WithDefaultColor()
        );
    }

    [Command("disable"),
        Description("Uninstalls/kills a running Lua task action."),
        RequireBotOwner]
    public async Task DisableHandlersAsync(
        CommandContext ctx,

        [SlashAutoCompleteProvider(typeof(ActionNameAutocomplete)),
            Description(ACTION_NAME_DESCRIPTION)]
        string actionNamesList)
    {
        GuildDbEntity? guild = await _dbContext.GetDbGuildAsync(ctx.Guild);
        if (guild is null)
        {
            return;
        }

        string[] actionNames = actionNamesList.Split(',');

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle($"Disabling {actionNames.Length} Task {"Action".Pluralize(actionNames.Length)}");

        foreach (string actionName in actionNames.Select(s => s.Trim()))
        {
            EventAction? action = guild.DefinedActions.Find(a => a.ActionName == actionName);

            if (action is null)
            {
                _ = embed.AddField("Error", $"Failed to find action task '{actionName}'");
                continue;
            }

            string status = BotEventLinker.KillTaskAction(action);
            _ = embed.AddField(status, $"{actionName} - `{action.EventName}`");
        }

        _ = await _dbContext.SaveChangesAsync();
        await ctx.RespondAsync(embed);
    }

    [Command("copy"),
        TextAlias("cp")]
    public async Task CopyHandlerAsync(
        CommandContext ctx,

        [Description(ACTION_NAME_DESCRIPTION),
            SlashAutoCompleteProvider(typeof(ActionNameAutocomplete))]
        string actionName,

        [Description("The guild ID to take the task action from.")]
        ulong idSource,

        [Description("The guild ID to place the task action into.")]
        ulong idDest)
    {
        GuildDbEntity? sourceGuild = await _dbContext.GetDbGuildAsync(idSource);

        if (sourceGuild is null)
        {
            await ctx.RespondAsync($"There is no guild in the DB with the ID {idSource} (no source)");
            return;
        }

        GuildDbEntity? targetGuild = await _dbContext.GetDbGuildAsync(idDest);

        if (targetGuild is null)
        {
            await ctx.RespondAsync($"There is no guild in the DB with the ID {idSource} (no dest)");
            return;
        }

        EventAction? action = sourceGuild.DefinedActions
            .Find(d => d.ActionName == actionName);

        if (action is null)
        {
            await ctx.RespondAsync($"Failed to find action '{actionName}' in the source guild.");
            return;
        }

        targetGuild.DefinedActions.Add(action);
        _ = await _dbContext.SaveChangesAsync();

        await ctx.RespondAsync($"Cloned action `{actionName} ({action.EventName})` from " +
            $"{(await ctx.Client.GetGuildAsync(idSource)).Name} to " +
            (await ctx.Client.GetGuildAsync(idDest)).Name);
    }
}
