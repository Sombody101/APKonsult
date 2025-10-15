using APKonsult.Configuration;
using APKonsult.Context;
using DSharpPlus;
using DSharpPlus.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Reflection;

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
        HandleArguments(args);

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
            await APKonsultServiceBuilder.RunAsync();
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

    private static void HandleArguments(string[] args)
    {

        if (args.Contains("--list-commands"))
        {
            const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            IEnumerable<Type> types = typeof(Program).Assembly.GetTypes().Where(type => !type.IsNested || type.DeclaringType?.GetCustomAttribute<CommandAttribute>() is null);

            foreach (Type type in types)
            {
                if (type.GetCustomAttribute<CommandAttribute>() is not null)
                {
                    foreach (Type subcommand in type.GetNestedTypes(FLAGS)
                        .Where(c => c.GetCustomAttribute<CommandAttribute>() is null))
                    {
                        
                    }

                    foreach (MethodInfo method in type.GetMethods(FLAGS)
                        .Where(m => m.GetCustomAttribute<CommandAttribute>() is null))
                    {

                    }
                }
                else
                {
                    foreach ((MethodInfo method, CommandAttribute attr) in type.GetMethods().Where(m => m.GetCustomAttribute<CommandAttribute>() is not null)
                        .Select(m => new Tuple<MethodInfo, CommandAttribute>(m, m.GetCustomAttribute<CommandAttribute>()!)))
                    {
                        Console.WriteLine($"{attr}: {method.Name}");
                    }
                }
            }

            Environment.Exit(0);
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
            options => options.UseSqlite(APKonsultServiceBuilder.DB_CONNECTION_STRING)
        ));

        return builder;
    }
}