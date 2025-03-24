using APKonsult.CommandChecks.Attributes;
using APKonsult.Commands.AutoCompleters;
using APKonsult.Helpers;
using APKonsult.Models;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using System.ComponentModel;

namespace APKonsult.Commands.Admin;

public partial class TaskAutomation
{
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
}
