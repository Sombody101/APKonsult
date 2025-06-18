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

    public const bool IS_DEBUG_BUILD =
#if DEBUG
        true;
#else
        false;
#endif

    public const string BUILD_TYPE = IS_DEBUG_BUILD
            ? "Debug"
            : "Release";

    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(
                $"{ChannelIDs.FILE_ROOT}/logs/blog-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Verbose
            )
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
            .CreateLogger();

        Log.Information($"Bot start @ {{Now}} ({BUILD_TYPE} build)", DateTime.Now);

#if DEBUG
        // The bot has restarted itself (via command), so wait for the previous instance
        // to finish saving data
        if (args.Length > 0 && args[0] is Shared.PREVIOUS_INSTANCE_ARG)
        {
            Log.Information("Launching from previous instance : Waiting 1,000ms...");
            await Task.Delay(1000);
            Log.Information("Starting bot.");
        }
#endif

        // Initialize webhook
        ConfigManager.Manager.LoadBotTokens();
        string webhook = ConfigManager.Manager.Tokens.DiscordWebhookUrl;

        if (string.IsNullOrWhiteSpace(webhook))
        {
            Log.Error("Webook URL is not set!");
        }
        else
        {
            WebhookClient = new DiscordWebhookClient();
            await WebhookClient.AddWebhookAsync(new Uri(webhook));
        }

        // On close, save files
        AppDomain.CurrentDomain.ProcessExit += (e, sender) =>
        {
            Log.Information("[Exit@ {Now}] Bot shutting down.", DateTime.Now);
        };

        try
        {
            // Start the bot
            await APKonsultBot.RunAsync();
        }
        catch (TaskCanceledException)
        {
            // Ignore
        }
        catch (Exception e)
        {
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