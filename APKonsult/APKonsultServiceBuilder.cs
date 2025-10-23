// #define FORCE_TRACE_LOGS // Forces trace logging, even on Release builds.
#define NO_LAVALINK

using APKonsult.CommandChecks;
using APKonsult.Configuration;
using APKonsult.Context;
using APKonsult.Services;
using APKonsult.Services.RegexServices;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace APKonsult;

internal static partial class APKonsultServiceBuilder
{
    public const string DB_CONNECTION_STRING = $"Data Source={ChannelIDs.FILE_ROOT}/db/APKonsult-bot.db";

    public static IServiceProvider Services { get; private set; } = null!;

    public static Stopwatch StartupTimer { get; private set; } = null!;

    public static async Task RunAsync()
    {
        StartupTimer = Stopwatch.StartNew();

        BotConfigModel config = ConfigManager.Manager.BotConfig;
        TokensModel tokens = ConfigManager.Manager.Tokens;

        if (string.IsNullOrWhiteSpace(tokens.TargetBotToken))
        {
#if DEBUG
            Log.Error("No bot debug token provided: '{Token}'", tokens.TargetBotToken);
#else
            Log.Error("No bot token provided: '{Token}'", tokens.TargetBotToken);
#endif
            Environment.Exit(1);
        }

        await Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((ctx, services) =>
            {
                services.AddLogging(loggingBuilder =>
                {
                    LogLevel logLevel =
#if DEBUG || FORCE_TRACE_LOGS
                        LogLevel.Trace;
#else
                        LogLevel.Warning;
#endif

                    Log.Information("Using log-level {LogLevel}", logLevel);
                });

                services.AddSingleton(config);
                services.AddSingleton<DiscordClientService>();
                services.AddHostedService(s => s.GetRequiredService<DiscordClientService>());

                services.AddDiscordClient(tokens.TargetBotToken, TextCommandProcessor.RequiredIntents
                    | SlashCommandProcessor.RequiredIntents
                    | DiscordIntents.AllUnprivileged
                    | DiscordIntents.MessageContents
                    | DiscordIntents.GuildMembers
                    | DiscordIntents.GuildEmojisAndStickers
                    | DiscordIntents.GuildVoiceStates);

                services.AddDbContextFactory<APKonsultContext>(
                    options =>
                    {
                        Log.Information("Adding SQLite DB service");
                        options.UseSqlite(DB_CONNECTION_STRING);
                    }
                );

                services.AddMemoryCache(options =>
                {
                    StringBuilder cacheInfo = new("Adding DB memory cache:");

                    cacheInfo.AppendLine()
                             .Append("\tCompaction:  ").AppendLine(options.CompactionPercentage.ToString())
                             .Append("\tScan Freq:   ").AppendLine(options.ExpirationScanFrequency.Humanize())
                             .Append("\tCache Limit: ").AppendLine(options.SizeLimit?.ToString() ?? "[No limit]");

                    Log.Information(cacheInfo.ToString());
                });

                services.AddSingleton(new AllocationRateTracker())
                    .AddSingleton(tokens);

                // Tracking regex cache and service
                services.AddScoped<IRegexCache, RegexCache>();
                services.AddScoped<IRegexService, RegexService>();

                services.AddSingleton(services =>
                {
                    return new HttpClient()
                    {
                        DefaultRequestHeaders = {
                            { "User-Agent", FormatUserAgentHeader(config.UserAgent) }
                        }
                    };
                });

                services.ConfigureEventHandlers(builder =>
                {
                    InitializeEvents(builder);

                    MethodInfo addEventHandlersMethod = builder.GetType()
                        .GetMethod(nameof(EventHandlingBuilder.AddEventHandlers), 1, [typeof(ServiceLifetime)])
                            ?? throw new InvalidOperationException("Failed to find AddEventHandlers method.");

                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        foreach (Type type in assembly.GetExportedTypes()
                            .Where(t => t.IsAssignableTo(typeof(IEventHandler)) && !t.IsAbstract))
                        {
                            addEventHandlersMethod.MakeGenericMethod(type).Invoke(builder, [ServiceLifetime.Singleton]);
                        }
                    }
                });

                CommandsConfiguration cConfig = new()
                {
                    UseDefaultCommandErrorHandler = false,
                };

                services.AddCommandsExtension((provider, cmdExt) =>
                {
                    cmdExt.AddProcessor(new TextCommandProcessor(new TextCommandConfiguration()
                    {
                        PrefixResolver = new DefaultPrefixResolver(true, [.. config.CommandPrefixes]).ResolvePrefixAsync,
                    }));

                    Assembly assembly = typeof(Program).Assembly;
                    cmdExt.AddCommands(assembly);
                    cmdExt.AddChecks(assembly);
                    cmdExt.CommandErrored += HandleCommandErroredAsync;

#if DEBUG
                    var types = assembly.GetTypes().SelectMany(t => t.GetMethods())
                        .Where(m => m.GetCustomAttribute<CommandAttribute>() is not null && m.GetCustomAttribute<UserGuildInstallableAttribute>() is null);
                    Log.Logger.Warning($"{{Count}} commands do not have {nameof(UserGuildInstallableAttribute)}", types.Count());
#endif
                }, cConfig);

                InteractivityConfiguration interactivityConfig = new()
                {
                    Timeout = TimeSpan.FromMinutes(10),
                    PollBehaviour = PollBehaviour.KeepEmojis,
                    ButtonBehavior = ButtonPaginationBehavior.DeleteButtons,
                    PaginationBehaviour = PaginationBehaviour.Ignore,
                    ResponseBehavior = InteractionResponseBehavior.Ignore,
                    PaginationDeletion = PaginationDeletion.DeleteEmojis
                };

                services.AddInteractivityExtension(interactivityConfig);

                Services = services.BuildServiceProvider();
            })
            .RunConsoleAsync();
    }

    /// <summary>
    /// Implement important Guild based events
    /// </summary>
    /// <param name="client"></param>
    private static void InitializeEvents(EventHandlingBuilder cfg)
    {
        _ = cfg.HandleModalSubmitted(async (client, sender) =>
        {
            await sender.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
        });

        _ = cfg.HandleGuildCreated(async (client, args) =>
        {
            await Task.Run(() => Log.Information("Joined guild: {Name} (id {Id})", args.Guild.Name, args.Guild.Id));
        });

        _ = cfg.HandleGuildAvailable(async (client, sender) =>
        {
            await Services.GetRequiredService<IRegexService>().RefreshCacheAsync(sender.Guild.Id);
        });

        _ = cfg.HandleZombied(async (client, args) => await client.ReconnectAsync());
    }

    private static async Task HandleCommandErroredAsync(CommandsExtension sender, CommandErroredEventArgs e)
    {
        if (e.Context.Command is not null)
        {
            string commandName = e.Context.Command.Name;
            string fullName = e.Context.Command.FullName;

            if (commandName != fullName)
            {
                Log.Error(e.Exception, "Given command: {CommandName} [full:{FullName}]", commandName, fullName);
            }
            else
            {
                Log.Error(e.Exception, "Given command: {CommandName}", commandName);
            }
        }
        else
        {
            Log.Error("Command not found: {Command}", e.CommandObject);
        }

        Exception ex = e.Exception.InnerException ?? e.Exception;

#if DEBUG
        if (e.Context.User.Id is ChannelIDs.ABSOLUTE_ADMIN)
        {
            foreach (var builder in ex.MakeEmbedFromException())
            {
                await sender.Client.SendMessageAsync(await sender.Client.GetChannelAsync(BotConfigModel.DEBUG_CHANNEL), builder);
            }
        }
#endif

        switch (ex)
        {
            case CommandNotFoundException cex:
                {
                    await e.Context.RespondAsync(new DiscordEmbedBuilder()
                        .WithTitle("Unknown command!")
                        .AddField(cex.CommandName, cex.Message)
                        .WithFooter("Use `/help` for a list of commands"));
                }
                break;

            case ArgumentParseException:
                {
                    await e.Context.RespondAsync(ex.Message);
                }
                break;

            case ChecksFailedException checks:
                {
                    DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                        .WithTitle("You cannot run this command!")
                        .WithColor(DiscordColor.Red);

                    StringBuilder sb = new();
                    foreach (DSharpPlus.Commands.ContextChecks.ContextCheckFailedData reason in checks.Errors)
                    {
                        sb.Append(reason.ErrorMessage).Append('\n');
                    }

                    embed.AddField("Reason", sb.ToString().TrimEnd());

                    await e.Context.RespondAsync(embed);
                }
                break;

            case BadRequestException brEx:
                {
                    DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                        .WithTitle("Bad Request")
                        .WithColor(DiscordColor.Red);

                    if (e.Context.User.IsOwner())
                    {
                        embed.AddField("Message", brEx.Message);
                    }
                    else
                    {
                        embed.WithDescription("Try again later!");
                    }

                    await e.Context.RespondAsync(embed);
                }
                break;

            default:
                {
                    await e.Context.RespondAsync("Uh oh!\nSomething went wrong!");
                }
                break;
        }
    }

    private static string FormatUserAgentHeader(string sourceHeader)
    {
        Assembly assembly = typeof(APKonsultServiceBuilder).Assembly;
        Dictionary<string, string> formatValues = new()
        {
            { "version", assembly.GetName().Version!.ToString() },
            { "osv", Environment.OSVersion.ToString() },
            { "buildtype", Program.BUILD_TYPE },
            // I don't see any reason for this to be anything other than x64, but it's nice to have.
            { "arch", RuntimeInformation.ProcessArchitecture.ToString() }
        };

        return TemplateRegex().Replace(sourceHeader, match =>
        {
            string key = match.Groups[1].Value;

            if (formatValues.TryGetValue(key, out string? value))
            {
                return value;
            }

            return match.Value;
        });
    }

    [GeneratedRegex(@"{(\w+)}")]
    private static partial Regex TemplateRegex();
}