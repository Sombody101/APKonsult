using APKonsult.CommandChecks.Attributes;
using APKonsult.Configuration;
using APKonsult.Context;
using APKonsult.Helpers;
using APKonsult.Models.Main;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

#if RELEASE
using Microsoft.Extensions.Logging;
#endif

namespace APKonsult.Commands.Admin;

[Command("manager"),
    TextAlias("manage"),
    Hidden,
    RequireBotOwner]
public sealed class BotManager(APKonsultContext _dbContext, HttpClient _httpClient)
{
    [Command("clearslashcommands"), RequireBotOwner]
    public static async ValueTask ClearSlashCommandsAsync(CommandContext ctx)
    {
        await ctx.Client.BulkOverwriteGlobalApplicationCommandsAsync([]);
        await ctx.RespondAsync("Slash commands overwritten.");
    }

    [Command("addadmin"),
        Description("Gives the specified user bot administrator status."),
        RequireBotOwner]
    public async Task AddAdminAsync(CommandContext ctx,
        [Description("The ID of the wanted user")] ulong userId)
    {
        DiscordUser? disUser = await ctx.Client.TryGetUserAsync(userId);

        if (disUser is null)
        {
            await ctx.RespondAsync("Failed to find a user by that ID!");
            return;
        }

        UserDbEntity? dbUser = await _dbContext.Users.FindAsync(userId);

        if (dbUser is null)
        {
            UserDbEntity new_user = new(disUser)
            {
                IsBotAdmin = true
            };

            _ = await _dbContext.Users.AddAsync(new_user);
        }
        else if (!dbUser.IsBotAdmin)
        {
            dbUser.IsBotAdmin = true;
        }
        else
        {
            await ctx.RespondAsync($"{disUser.Username} is already an administrator!");
            return;
        }

        _ = await _dbContext.SaveChangesAsync();
        await ctx.RespondAsync($"{disUser.Mention} is now registered as a bot administrator.");
    }

    [Command("removeadmin"),
        Description("Removes bot administrator status from the specified user"),
        Hidden,
        RequireBotOwner]
    public async ValueTask RemoveAdminAsync(CommandContext ctx, ulong userId)
    {
        DiscordUser? disUser = await ctx.Client.TryGetUserAsync(userId);

        if (disUser is null)
        {
            await ctx.RespondAsync("Failed to find a user by that ID!");
            return;
        }

        UserDbEntity? dbUser = await _dbContext.Users.FindAsync(userId);

        if (dbUser is null)
        {
            UserDbEntity newUser = new(disUser)
            {
                IsBotAdmin = false
            };

            _ = await _dbContext.Users.AddAsync(newUser);
        }
        else if (dbUser.IsBotAdmin)
        {
            dbUser.IsBotAdmin = false;
        }
        else
        {
            await ctx.RespondAsync($"{disUser.Username} wasn't an administrator!");
            return;
        }

        _ = await _dbContext.SaveChangesAsync();
        await ctx.RespondAsync($"{disUser.Username} is no longer a bot administrator.");
    }

    [Command("listadmins"), Hidden, RequireBotOwner]
    public async ValueTask ListAdminsAsync(CommandContext ctx)
    {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder().WithTitle("Active Administrators");
        IQueryable<UserDbEntity> admins = _dbContext.Users.Where(user => user.IsBotAdmin);

        if (!await admins.AnyAsync())
        {
            _ = embed.AddField("There currently zero administrators", $"User count: `{await _dbContext.Users.CountAsync()}`");
            await ctx.RespondAsync(embed);
            return;

        }

        foreach (UserDbEntity? user in admins)
        {
            _ = embed.AddField(user.Username, user.Id.ToString());
        }

        await ctx.RespondAsync(embed);
    }

    /* Bot owner commands */

    [Command("addprefix"),
        Description("Adds a prefix to the bots configuration (requires restart)."),
        Hidden,
        RequireBotOwner]
    public static async Task AddPrefixAsync(CommandContext ctx, params string[] prefixes)
    {
        BotConfigModel config = ConfigManager.Manager.BotConfig;

        foreach (string prefix in prefixes)
        {
            if (config.CommandPrefixes.Contains(prefix))
            {
                await ctx.RespondAsync($"The prefix `{prefix}` is already in use!");
            }
            else
            {
                config.CommandPrefixes.Add(prefix);
            }
        }

        await ConfigManager.Manager.SaveBotConfigAsync();
        await ctx.RespondAsync($"Added {prefixes.Length} {"prefix".Pluralize(prefixes.Length)}.\nChanges will be installed on next restart.");
    }

    [Command("restart"),
        Description("Restarts the bot."),
        Hidden,
        RequireBotOwner]
    public static async ValueTask RestartAsync(CommandContext ctx, int exit_code = 0)
    {
        string openPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{AppDomain.CurrentDomain.FriendlyName}.exe");

        await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
            .WithTitle("Restarting")
            .WithColor(Shared.DefaultEmbedColor)
            .AddField("Exit Code", exit_code.ToString())
            .AddField("Restart Location", openPath)
            .AddField("Restart Time", DateTime.Now.ToString())
            .AddField("Restart Time UTC", DateTime.UtcNow.ToString())
            .WithFooter("Restart will take ~1000ms to account for file stream exits and bot initialization.")
        );

#if DEBUG
        // Docker should restart APKonsult automatically
        System.Diagnostics.Process.Start(openPath, Shared.PREVIOUS_INSTANCE_ARG);
#endif

        Environment.Exit(exit_code);
    }

    [Command("agent"), Hidden, RequireAdminUser]
    public async Task GetUserAgentAsync(CommandContext ctx)
    {
        string header = _httpClient.DefaultRequestHeaders.UserAgent.ToString();
        await ctx.RespondAsync($"```\n{header}\n```");
    }

    /*
     * Blacklist Commands
     */

    [Command("blacklist"), Hidden, RequireBotOwner]
    public sealed class BlacklistManager(APKonsultContext _dbContext)
    {
        [Command("user"),
            DefaultGroupCommand,
            Hidden,
            RequireBotOwner]
        public async ValueTask BlacklistMemberAsync(
            CommandContext ctx,

            DiscordUser user,

            [RemainingText]
            string? reason = null)
        {
            BlacklistedDbEntity? activeUser = await _dbContext.Set<BlacklistedDbEntity>()
                .Where(bl => bl.UserId == user.Id)
                .FirstOrDefaultAsync();

            if (activeUser is not null)
            {
                await ctx.RespondAsync("This user is already banned.\n" + activeUser.BanReason());
                return;
            }

            BlacklistedDbEntity newlist = new()
            {
                UserId = user.Id,
                Reason = reason ?? string.Empty
            };

            _ = await _dbContext.AddAsync(newlist);
            _ = await _dbContext.SaveChangesAsync();

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithTitle("User Added To Blacklist")
                .WithColor(DiscordColor.Red)
                .AddField("User", $"{user.Username} (`{user.Id}`)")
                .AddField("Reason", newlist.BanReason()));
        }

        [Command("userid"), Hidden, RequireBotOwner]
        public async ValueTask BlacklistMemberAsync(
            CommandContext ctx,

            [Description("The ID of the user to blacklist.")]
            ulong user_id,

            [Description("An optional reason as to why this user cannot use APKonsult."), RemainingText]
            string? reason = null)
        {
            DiscordUser? user = await ctx.Client.TryGetUserAsync(user_id);

            if (user is null)
            {
                await ctx.RespondAsync($"Failed to find a user by the ID `{user_id}`.");
                return;
            }

            await BlacklistMemberAsync(ctx, user, reason);
        }

        [Command("unblacklist"), Hidden, RequireBotOwner]
        public async ValueTask UnblacklistMemberAsync(CommandContext ctx, DiscordUser user)
        {
            BlacklistedDbEntity? activeUser = await _dbContext.Set<BlacklistedDbEntity>()
                .Where(bl => bl.UserId == user.Id)
                .FirstOrDefaultAsync();

            if (activeUser is null)
            {
                await ctx.RespondAsync("This user is not currently banned.");
                return;
            }

            _ = _dbContext.Remove(activeUser);
            _ = await _dbContext.SaveChangesAsync();

            await ctx.RespondAsync($"{user.Username} has been removed from the blacklist.");
        }

        public async ValueTask UnblacklistMemberAsync(CommandContext ctx, ulong user_id)
        {
            DiscordUser? user = await ctx.Client.TryGetUserAsync(user_id);

            if (user is null)
            {
                await ctx.RespondAsync($"Failed to find a user by the ID `{user_id}`.");
                return;
            }

            await UnblacklistMemberAsync(ctx, user);
        }

        [Command("guild"), Hidden, RequireBotOwner]
        public async ValueTask BlacklistGuildAsync(CommandContext ctx, ulong guild_id, [RemainingText] string? reason = null)
        {
            DiscordGuild? guild = await ctx.Client.TryGetGuildAsync(guild_id);

            if (guild is null)
            {
                await ctx.RespondAsync("Failed to find a guild by that ID!");
                return;
            }

            BlacklistedDbEntity? activeGuild = await _dbContext.Set<BlacklistedDbEntity>()
                .Where(bl => bl.GuildId == guild.Id)
                .FirstOrDefaultAsync();

            if (activeGuild is not null)
            {
                await ctx.RespondAsync("This guild is already banned.\n" + activeGuild.BanReason());
                return;
            }

            BlacklistedDbEntity newlist = new()
            {
                GuildId = guild.Id,
                Reason = reason ?? string.Empty
            };

            _ = await _dbContext.AddAsync(newlist);
            _ = await _dbContext.SaveChangesAsync();

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithTitle("User Added To Blacklist")
                .WithColor(DiscordColor.Red)
                .AddField("Guild", $"{guild.Name} (`{guild.Id}`)")
                .AddField("Reason", newlist.BanReason()));
        }

        public async ValueTask UnblacklistGuildAsync(CommandContext ctx, DiscordGuild guild)
        {
            BlacklistedDbEntity? activeUser = await _dbContext.Set<BlacklistedDbEntity>()
                .Where(bl => bl.GuildId == guild.Id)
                .FirstOrDefaultAsync();

            if (activeUser is null)
            {
                await ctx.RespondAsync("This user is not currently banned.");
                return;
            }

            _ = _dbContext.Remove(activeUser);
            _ = await _dbContext.SaveChangesAsync();

            await ctx.RespondAsync($"{guild.Name} has been removed from the blacklist.");
        }
    }


    [Command("downloaddb"), Hidden, RequireBotOwner]
    public async ValueTask DownloadDatabaseAsync(TextCommandContext ctx)
    {
        string spareName = $"{ChannelIDs.FILE_ROOT}/spare.{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.db";
        try
        {
            var sourceConnection = _dbContext.Database.GetDbConnection();

            if (sourceConnection is not SqliteConnection connection)
            {
                await ctx.RespondAsync("Failed to get SQLiteDb connection.");
                return;
            }

            if (connection.State is not System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var backupConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = spareName,
                Pooling = false,
            }.ToString();

            using (var backupConnection = new SqliteConnection(backupConnectionString))
            {
                await backupConnection.OpenAsync();
                connection.BackupDatabase(backupConnection);
            }

            using FileStream reader = File.OpenRead(spareName);

            var channel = await ctx.GetDmChannelAsync();
            await channel.SendMessageAsync(new DiscordMessageBuilder().AddFile(reader));
        }
        catch (Exception ex)
        {
            await ex.LogToWebhookAsync(this);
        }
        finally
        {
            if (File.Exists(spareName))
            {
                File.Delete(spareName);
            }
        }
    }

    [Command("update"), Hidden, RequireBotOwner]
    public sealed class UpdateManager(
#if RELEASE
        HttpClient _httpClient, TokensModel tokens, ILogger<UpdateManager> _logger
#endif
        )
    {
        [Command("update"),
            TextAlias("upgrade"),
            DefaultGroupCommand,
            Hidden,
            RequireBotOwner]
#if DEBUG
        public static async Task InvokeWatchtowerUpdateAsync(CommandContext ctx)
        {
            await ctx.RespondAsync("Updates are only available on release builds.");
        }
#else
        public async Task InvokeWatchtowerUpdateAsync(CommandContext ctx)
        {
            const string WATCHTOWER_URL = $"http://watchtower:8080/v1/update";
            string token = tokens.WatchtowerToken;

            if (token == string.Empty)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithTitle("Update failed.")
                    .WithDescription("No Watchtower token set.")
                    .WithColor(DiscordColor.Red)
                );

                return;
            }

            try
            {
                await ctx.DeferResponseAsync();

                using var request = new HttpRequestMessage(HttpMethod.Post, WATCHTOWER_URL);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    await ctx.RespondAsync(new DiscordEmbedBuilder()
                        .WithTitle("Request Failed")
                        .AddDefaultField("Status Code", response.StatusCode.ToString())
                        .AddDefaultField("Response", await response.Content.ReadAsStringAsync())
                    );
                }

                // If it even lasts long enough to send this...
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithTitle("Update Triggered")
                    .AddDefaultField("Response", await response.Content.ReadAsStringAsync())
                );
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Exception: Could not connect to Watchtower API");
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithTitle("Request Failed")
                    .WithDescription(ex.Message)
                    .WithColor(DiscordColor.Red)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error.");
                await ctx.RespondAsync($"Unexpected error: {ex.Message}");
                await ex.LogToWebhookAsync();
            }
        }
#endif
    }

    [Command("secret"),
        TextAlias("secrets"),
        Hidden,
        RequireBotOwner]
    public sealed class SecretsManager(TokensModel tokens)
    {
        [Command("get"),
            TextAlias("print"),
            DefaultGroupCommand,
            Hidden,
            RequireBotOwner]
        public async Task SecretlyShowSecretAsync(TextCommandContext ctx, string item)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                await ctx.RespondAsync("Unexpected error.");
                return;
            }

            string token = item switch
            {
                "bottoken" => tokens.TargetBotToken,
                "watchtoken" => tokens.WatchtowerToken,
                _ => "[NONE]"
            };

            var dm = await ctx.GetDmChannelAsync();
            await dm.SendMessageAsync(new DiscordEmbedBuilder()
                .WithTitle("Token")
                .WithDescription(token)
                .AddField("Size", $"{GBConverter.FormatSizeFromBytes(token.Length)} ({token.Length:n})")
                .WithColor(DiscordColor.Red)
            );
        }
    }

    [Command("crash"), Hidden, RequireBotOwner, DoesNotReturn]
    [SuppressMessage("Major Bug", "S1764:Identical expressions should not be used on both sides of operators", Justification = "No.")]
    public static Task TestCrash(TextCommandContext ctx, int limit = 10, int count = 0)
    {
        if (count <= limit)
        {
            // Just to inflate the call stack a bit.
            _ = TestCrash(ctx, limit, count + 1);
            return null!;
        }

        throw new TestException();
    }

    public sealed class TestException() : Exception("This is a command crash test exception.") { }
}