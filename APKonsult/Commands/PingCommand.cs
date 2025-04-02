using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Entities;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace APKonsult.Commands;

public static class PingCommand
{
    [Command("ping"),
        Description("Pings the bot and returns the gateway latency.")]
    public static async Task PingAsync(CommandContext ctx)
    {
        TimeSpan latency = ctx.Client.GetConnectionLatency(ctx.Guild!.Id);

        await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
            .WithTitle(Random.Shared.Next() % 3948 == 0
                ? "Pongie!"
                : "Pong!")
            .WithColor()
            .AddField($"Response latency", $"{latency.Milliseconds}ms ({latency.TotalMilliseconds}ms)"));
    }

    [Command("uptime"),
        Description("Get the bots uptime")]
    public static async Task UptimeAsync(CommandContext ctx)
    {
        await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
            .WithTitle("Uptime")
            .WithColor()
            .WithDescription(FormatTickCount()));
    }

    [Command("echo"),
        Description("Makes the bot create a message with your text")]
    public static async Task EchoAsync(
        CommandContext ctx,

        [Description("The text you want APKonsult to reply with."),
            RemainingText]
        string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        await ctx.RespondAsync(message);
    }

    [Command("embed"),
        Description("The same as 'echo', but prints the text in an embed")]
    public static async Task EchoEmbedAsync(
        CommandContext ctx,

        [Description("The test you want APKonsult to reply with via an embed."),
            RemainingText]
        string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        await ctx.RespondAsync(new DiscordEmbedBuilder().WithDescription(message));
    }

    public static string FormatTickCount()
    {
        TimeSpan uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

        int days = uptime.Days;
        int hours = uptime.Hours;
        int minutes = uptime.Minutes;
        int seconds = uptime.Seconds;

        StringBuilder output = new();

        if (days is not 0)
        {
            _ = output.Append(days)
                .Append(" day")
                .Append('s'.Pluralize(days))
                .Append(", ");
        }

        if (hours is not 0)
        {
            _ = output.Append(hours)
                .Append(" hour")
                .Append('s'.Pluralize(hours))
                .Append(", ");
        }

        if (minutes is not 0)
        {
            _ = output.Append(minutes)
                .Append(" minute")
                .Append('s'.Pluralize(minutes))
                .Append(", ");
        }

        if (seconds is not 0)
        {
            _ = output.Append(seconds)
                .Append(" second")
                .Append('s'.Pluralize(seconds));
        }

        return $"{output} ({uptime.TotalMilliseconds:n0}ms)";
    }
}