using APKonsult.CommandChecks.Attributes;
using APKonsult.Commands.AutoCompleters;
using APKonsult.Helpers;
using APKonsult.Models;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using System.ComponentModel;

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

        _ = embed.WithTitle($"Deploying {actionsToDeploy.Count} Task Action{"s".Pluralize(actionsToDeploy.Count)}");

        foreach (EventAction action in actionsToDeploy)
        {
            string status = BotEventLinker.DeployTaskAction(ctx.Guild!, action);
            (int code, long initMs) = BotEventLinker.InvokeScript(action, null, ctx.Guild);
            _ = embed.AddField(status, $"{action.ActionName} - `{action.EventName}` ({GBConverter.FormatSizeFromBytes(action.LuaScript.Length)})\n" +
                $"Init returned {code} and took {initMs}ms.");

            if (code is 0)
            {
                action.Enabled = true;
            }
        }

        _ = await _dbContext.SaveChangesAsync();
        await ctx.RespondAsync(embed);
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
            .WithTitle($"Disabling {actionNames.Length} Task Action{"s".Pluralize(actionNames.Length)}");

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
