using APKonsult.Configuration;
using APKonsult.Context;
using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace APKonsult;

internal static class Program
{
    public static DiscordWebhookClient WebhookClient { get; set; } = null!;

    public const bool DebugBuild =
#if DEBUG
        true;
#else
        false;
#endif

    public const string BuildType = DebugBuild
            ? "Debug"
            : "Release";

    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(
                "logs/bot.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information
            ).MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .CreateLogger();

        Log.Information($"Bot start @ {{Now}} ({BuildType} build)", DateTime.Now);

#if DEBUG
        // The bot has restarted itself (via command), so wait for the previous instance
        // to finish saving data
        if (args.Length > 0 && args[0] is Shared.PREVIOUS_INSTANCE_ARG)
        {
            Log.Information("Launching from previous instance : Waiting 1000ms...");
            await Task.Delay(1000);
            Log.Information("Starting bot.");
        }
#endif

        // Initialize webhook
        WebhookClient = new DiscordWebhookClient();
        Uri webhookUrl = new(ConfigManager.Manager.BotConfig.DiscordWebhookUrl);
        await WebhookClient.AddWebhookAsync(webhookUrl);

        // On close, save files
        AppDomain.CurrentDomain.ProcessExit += (e, sender) =>
        {
            Log.Information("[Exit@ {Now}] Saving all configs...", DateTime.Now);

            // Ensure all configs are saved
            ConfigManager.Manager.SaveBotConfig().Wait();
        };

        try
        {
            // Start the bot
            await APKonsultBot.RunAsync();
        }
        catch (Exception e)
        {
            if (e is TaskCanceledException)
            {
                return;
            }

            await e.LogToWebhookAsync();
            Environment.Exit(69);
        }
    }

    /// <summary>
    /// Used for ECF CLI Migration tools
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        IHostBuilder builder = Host.CreateDefaultBuilder(args);

        _ = builder.ConfigureServices((_, services) => services.AddDbContextFactory<APKonsultContext>(
            options => options.UseSqlite(APKonsultBot.DB_CONNECTION_STRING)
        ));

        return builder;
    }
}