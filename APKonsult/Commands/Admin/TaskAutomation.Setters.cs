using APKonsult.CommandChecks.Attributes;
using APKonsult.Commands.AutoCompleters;
using APKonsult.Models;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using System.ComponentModel;

namespace APKonsult.Commands.Admin;

public partial class TaskAutomation
{
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
        string script = "")
    {
        if (await _dbContext.GetDbGuildAsync(ctx.Guild) is not GuildDbEntity guild)
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
        if (await _dbContext.GetDbGuildAsync(ctx.Guild) is not GuildDbEntity guild)
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
        if (await _dbContext.GetDbGuildAsync(ctx.Guild) is not GuildDbEntity guild)
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
}
