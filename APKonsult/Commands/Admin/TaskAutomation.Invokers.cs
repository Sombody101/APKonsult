﻿using APKonsult.CommandChecks.Attributes;
using APKonsult.Commands.AutoCompleters;
using APKonsult.Models;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using System.ComponentModel;

namespace APKonsult.Commands.Admin;

public partial class TaskAutomation
{
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
}
