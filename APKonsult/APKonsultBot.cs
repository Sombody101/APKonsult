// #define FORCE_TRACE_LOGS // Forces trace logging, even on Release builds.

using APKonsult.Commands.Admin.TaskRunner.FunctionBindings;
using APKonsult.Configuration;
using APKonsult.Context;
using APKonsult.EventHandlers;
using APKonsult.Models;
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
using DSharpPlus.EventArgs;
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
using System.Text;

namespace APKonsult;

internal static class APKonsultBot
{
    public const string DB_CONNECTION_STRING = $"Data Source={ChannelIDs.FILE_ROOT}/db/APKonsult-bot.db";

    public static IServiceProvider Services { get; private set; } = null!;

    public static Stopwatch StartupTimer { get; private set; } = null!;

    public static async Task RunAsync()
    {
        StartupTimer = Stopwatch.StartNew();

        BotConfigModel config = ConfigManager.Manager.BotConfig;

        string token =
#if DEBUG
            config.DebugBotToken;
#else
            config.BotToken;
#endif

        if (string.IsNullOrWhiteSpace(token))
        {
#if DEBUG
            Log.Error("No bot debug token provided: '{Token}'", token);
#else
            Log.Error("No bot token provided: '{Token}'", token);
#endif
            Environment.Exit(1);
        }

        await Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((ctx, services) =>
            {
                services.AddLogging(logging =>
                {
                    LogLevel logLevel =
#if DEBUG || FORCE_TRACE_LOGS
                        LogLevel.Trace;
#else
                        LogLevel.Warning;
#endif

                    logging.SetMinimumLevel(logLevel)
                        .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning)
                        .AddConsole();

                    Log.Information("Using log-level {LogLevel}", logLevel);
                });

                services.AddSingleton(config);
                services.AddSingleton<DiscordClientService>();
                services.AddHostedService(s => s.GetRequiredService<DiscordClientService>());

                services.AddDiscordClient(token, TextCommandProcessor.RequiredIntents
                    | SlashCommandProcessor.RequiredIntents
                    | DiscordIntents.MessageContents
                    | DiscordIntents.GuildMembers
                    | DiscordIntents.GuildEmojisAndStickers);

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

                services.AddSingleton(new AllocationRateTracker());

                // Tracking regex cache and service
                services.AddScoped<IRegexCache, RegexCache>();
                services.AddScoped<IRegexService, RegexService>();

                services.AddSingleton(services =>
                {
                    return new HttpClient()
                    {
                        DefaultRequestHeaders = {
                            { "User-Agent", config.GitHubUserAgent }
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
                    DebugGuildId = 0,//ChannelIDs.DEBUG_GUILD_ID,
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

        _ = cfg.HandleGuildMemberRemoved(async (client, args) =>
        {
            // My server
            if (!Program.IS_BEBUG_GUILD && args.Guild.Id == ChannelIDs.DEBUG_GUILD_ID)
            {
                DiscordChannel channel = await client.GetChannelAsync(ChannelIDs.CHANNEL_GENERAL);
                _ = await channel.SendMessageAsync($"{args.Member.Mention} left the server!");
            }
        });

        _ = cfg.HandleMessageCreated(HandleMessageCreatedAsync);

        _ = cfg.HandleGuildAvailable(async (client, sender) =>
        {
            await Services.GetRequiredService<IRegexService>().RefreshCacheAsync(sender.Guild.Id);
        });

        _ = cfg.HandleZombied(async (client, args) => await client.ReconnectAsync());

        _ = cfg.HandleGuildAvailable(async (client, args) => await Task.Run(() => Log.Information("Guild available: {Name}", args.Guild.Name)));
    }

    private static async Task HandleMessageCreatedAsync(DiscordClient client, MessageCreatedEventArgs args)
    {
        APKonsultContext db = (await Shared.TryGetDbContextAsync())!;

        DiscordMessage message = args.Message;
        if (message.Author is null)
        {
            return;
        }

        UserDbEntity? user = await db.Users.FindAsync(message.Author.Id);
        if (user is null)
        {
            // User is not in DB
            return;
        }

        if (!args.Author.IsBot)
        {
            // Tracking service
            await Services.GetRequiredService<IRegexService>().UseRegexAsync(args.Guild.Id, args.Channel.Id, args.Message);

            List<AfkStatusEntity> afkUsers = await db.Set<AfkStatusEntity>()
                .Where(x => x.UserId == args.Author.Id
                     || args.MentionedUsers.Select(u => u.Id).Contains(x.UserId))
                .ToListAsync();

            // Check AFK
            AfkStatusEntity? authorAfk = afkUsers.Find(x => x.UserId == args.Author.Id);
            if (authorAfk.IsAfk() && args.Message.Content.Length > 5)
            {
                // Message is larger than 5 characters
                user.AfkStatus = null;
                _ = await db.SaveChangesAsync();
                _ = await args.Message.RespondAsync($"Welcome back {args.Author.Mention}!\nI've removed your AFK status.");
            }

            // Respond with AFK users and when they went AFK
            if (args.MentionedUsers.Any())
            {
                StringBuilder sb = new();

                IEnumerable<AfkStatusEntity> afkMentionedUsers = afkUsers.Where(x => x.UserId != args.Author.Id && x.IsAfk());
                if (afkMentionedUsers.Any())
                {
                    foreach (AfkStatusEntity? afkUser in afkMentionedUsers)
                    {
                        _ = sb.AppendLine($"<@{afkUser.UserId}> went afk <t:{afkUser.AfkEpoch}:R>: {afkUser.AfkMessage}");
                    }
                }

                if (sb.Length > 0)
                {
                    _ = await args.Message.RespondAsync(sb.ToString());
                }
            }
        }

        await HandleTagEvent.HandleTagAsync(client, args, db);

        string? emojiStr = user.ReactionEmoji;

        if (string.IsNullOrWhiteSpace(emojiStr))
        {
            return;
        }

        try
        {
            if (!DiscordEmoji.TryFromName(client, emojiStr, out DiscordEmoji? emoji))
            {
                Log.Error("Failed to locate emoji");
                return;
            }

            await message.CreateReactionAsync(emoji);
        }
        catch (Exception ex)
        {
            await ex.PrintExceptionAsync();
        }
    }

    private static async Task HandleCommandErroredAsync(CommandsExtension sender, CommandErroredEventArgs e)
    {
        string commandName = e.Context.Command?.Name ?? "$NULL";
        string fullName = e.Context.Command?.FullName ?? "$NULL";
        Log.Error(e.Exception, "Given command: {CommandName} [full:{FullName}]", commandName, fullName);

        Exception ex = e.Exception.InnerException ?? e.Exception;

#if DEBUG
        if (e.Context.User.Id is ChannelIDs.ABSOLUTE_ADMIN)
        {
            await sender.Client.SendMessageAsync(await sender.Client.GetChannelAsync(BotConfigModel.DebugChannel), ex.MakeEmbedFromException());
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
}