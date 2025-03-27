using APKonsult.Configuration;
using APKonsult.Context;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Diagnostics;

namespace APKonsult.Services;

internal class DiscordClientService : IHostedService
{
    public static DiscordClientService? StaticInstance { get; private set; }
    public static IDbContextFactory<APKonsultContext>? DbContextFactory { get; private set; }

    public readonly DiscordClient Client;
    public DateTime StartTime;

    public DiscordClientService
    (
        DiscordClient discordClient,
        IDbContextFactory<APKonsultContext> dbContextFactory
    )
    {
        StaticInstance = this;

        Client = discordClient;
        StartTime = DateTime.Now;
        DbContextFactory = dbContextFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("DiscordClientService started");
        await using APKonsultContext context = await DbContextFactory!.CreateDbContextAsync(cancellationToken);
        IEnumerable<string> pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);

        if (pendingMigrations.Any())
        {
            Stopwatch sw = Stopwatch.StartNew();

            await context.Database.MigrateAsync(cancellationToken);

            sw.Stop();
            Log.Information("Applied pending migrations in {ElapsedMilliseconds:n0} ms", sw.ElapsedMilliseconds);
        }

        Log.Information("Connecting bot");
        DiscordActivity status = new("for some commands", DiscordActivityType.Watching);
        await Client.ConnectAsync(status);

        try
        {
            System.Reflection.Assembly assembly = typeof(APKonsultBot).Assembly;

            APKonsultBot.StartupTimer.Stop();

            DiscordEmbedBuilder init_embed = new DiscordEmbedBuilder()
                .WithTitle("APKonsult Bot Active")
                .WithColor(DiscordColor.SpringGreen)
                .AddField("Start time", $"{APKonsultBot.StartupTimer.ElapsedMilliseconds:n0}ms", true)
                .AddField("Tick count", $"{APKonsultBot.StartupTimer.ElapsedTicks:n0} ticks", true)
                .AddField("Bot version", $"v{assembly.GetName().Version}", true)
                .AddField("Build type", $"***{Program.BUILD_TYPE}***", true)
                .AddField("Runtime version", $"R{assembly.ImageRuntimeVersion}", true)
                .MakeWide();

            _ = await Client.SendMessageAsync(await Client.GetChannelAsync(BotConfigModel.DebugChannel), init_embed);
        }
        catch (Exception ex)
        {
            Log.Information(ex, "Failed to send message to debug guild channel: {DebugChannel}", BotConfigModel.DebugChannel);
            await ex.LogToWebhookAsync();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Client.DisconnectAsync();
    }

    public static DiscordClient GetClient()
    {
        return StaticInstance!.Client;
    }
}