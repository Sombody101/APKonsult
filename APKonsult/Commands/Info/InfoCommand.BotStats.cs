using APKonsult.CommandChecks.Attributes;
using APKonsult.Services;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace APKonsult.Commands.Info;

public partial class InfoCommand
{
    private static readonly string _operatingSystem = $"{Environment.OSVersion} {RuntimeInformation.OSArchitecture.ToString().ToLower(CultureInfo.InvariantCulture)}";
    private static readonly string _botVersion = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
    private static readonly string _dSharpPlusVersion = typeof(DiscordClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

    [GeneratedRegex(", (?=[^,]*$)", RegexOptions.Compiled)]
    private static partial Regex _getLastCommaRegex();

    private readonly AllocationRateTracker _allocationRateTracker;

    [Command("bot"),
        Description("Get statistics about the bot"),
        MadeBy(Creator.Lunar)]
    public async Task GetBotStatsAsync(CommandContext ctx)
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();

        var embedBuilder = new DiscordEmbedBuilder()
            .WithTitle("Bot Info")
            .WithColor(Shared.DefaultEmbedColor)

        // Process data
            .AddField("Heap Memory", GC.GetTotalMemory(false).Bytes().ToString(CultureInfo.InvariantCulture), true)
            .AddField("Process Memory", process.WorkingSet64.Bytes().ToString(CultureInfo.InvariantCulture), true)
            .AddField("Allocation Rate", $"{_allocationRateTracker.AllocationRate.Bytes().ToString(CultureInfo.InvariantCulture)}/s", true)

        // Runtime & Machine info
            .AddField("Runtime Version", RuntimeInformation.FrameworkDescription, true)
            .AddField("Operating System", _operatingSystem, true)
            .AddField("Uptime", _getLastCommaRegex().Replace((Process.GetCurrentProcess().StartTime - DateTime.Now).Humanize(3), ", and "), true)
            .AddField("Threads", $"{ThreadPool.ThreadCount}", true);

        var latency = ctx.Client.GetConnectionLatency(0);
        string latency_value = "N/A (Wait for next heartbeat)";

        if (latency.TotalMilliseconds is not 0)
            latency_value = $"{latency.TotalMilliseconds:.00} ms";

        embedBuilder.AddField("Websocket Latency", latency_value, true);

        // Db data
        _ = await _dbContext.Users.FirstOrDefaultAsync();
        var swDb = Stopwatch.StartNew();
        _ = await _dbContext.Guilds.FirstOrDefaultAsync();
        swDb.Stop();

        embedBuilder.AddField("DB Latency:", $"{swDb.ElapsedMilliseconds:n0} ms", true)
            .AddField("DB Size", BytesToString(new FileInfo("./configs/APKonsult-bot.db").Length));

        int members = await _dbContext.Users.CountAsync();
        int guilds = await _dbContext.Guilds.CountAsync();

        embedBuilder.AddField("Member Count:", $"{members:n0}", true)
            .AddField("Guild Count:", $"{guilds:n0}", true);

        var sb = new StringBuilder();
        sb.Append(ctx.Client.CurrentUser.Mention).Append(", ");
        foreach (string prefix in ((DefaultPrefixResolver)ctx.Extension.GetProcessor<TextCommandProcessor>().Configuration.PrefixResolver.Target!).Prefixes)
        {
            sb.Append('`')
                .Append(prefix)
                .Append("`, ");
        }

        sb.Append(" `/`");
        embedBuilder.AddField("Prefixes", sb.ToString(), true)
            .AddField("Bot Version", _botVersion, true)
            .AddField("DSharpPlus Library Version", _dSharpPlusVersion, true);

        var responseBuilder = new DiscordInteractionResponseBuilder()
            .AddEmbed(embedBuilder).AsEphemeral();

        await ctx.RespondAsync(responseBuilder);
    }

    public static string BytesToString(long value)
    {
        string suffix;
        double readable;
        switch (Math.Abs(value))
        {
            case >= 0x40000000:
                suffix = "GB";
                readable = value >> 20;
                break;

            case >= 0x100000:
                suffix = "MB";
                readable = value >> 10;
                break;

            case >= 0x400:
                suffix = "KB";
                readable = value;
                break;

            default:
                return value.ToString("0 B");
        }

        return $"{readable / 1024:0.##}{suffix}";
    }
}