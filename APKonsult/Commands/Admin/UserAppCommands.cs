using APKonsult.CommandChecks;
using APKonsult.CommandChecks.Attributes;
using APKonsult.Commands.Info;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Diagnostics.CodeAnalysis;

namespace APKonsult.Commands.Admin;

/// <summary>
/// These are commands only I can use.
/// </summary>
public static class UserAppCommands
{
    [Command("uping"),
        UserGuildInstallable,
        RequireBotOwner]
    public static async Task PingAsync(SlashCommandContext ctx)
    {
        await PingCommand.PingAsync(ctx);
    }

    [Command("ubotinfo"),
        UserGuildInstallable,
        RequireBotOwner]
    public static async Task BotInfoAsync(SlashCommandContext ctx)
    {
        if (!GetCommandClass<InfoCommand>(out var infoCommand))
        {
            await ctx.RespondAsync("Failed to initialize command class!");
            return;
        }

        await infoCommand.GetBotStatsAsync(ctx);
    }

    private static bool GetCommandClass<T>([NotNullWhen(true)] out T? commandClass) where T : class
    {
        try
        {
            // No caching stuff here. Just rawdog
            commandClass = ActivatorUtilities.CreateInstance<T>(APKonsultServiceBuilder.Services);
            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to create command context.");
            commandClass = null;
            return false;
        }
    }
}
