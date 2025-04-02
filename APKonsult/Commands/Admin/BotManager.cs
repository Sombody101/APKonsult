﻿using APKonsult.CommandChecks.Attributes;
using APKonsult.Configuration;
using APKonsult.Context;
using APKonsult.Models;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using System.ComponentModel;

namespace APKonsult.Commands.Admin;

public class BotManager
{
    private readonly APKonsultContext _dbContext;

    public BotManager(APKonsultContext context)
    {
        _dbContext = context;
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
            UserDbEntity new_user = new()
            {
                Username = disUser.Username,
                Id = disUser.Id,
                IsBotAdmin = true, // Add user as an admin
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
            UserDbEntity newUser = new()
            {
                Username = disUser.Username,
                Id = disUser.Id,
                IsBotAdmin = false, // Set to false
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

    [Command("listadmins"), RequireBotOwner]
    public async ValueTask ListAdminsAsync(CommandContext ctx)
    {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder().WithTitle("Active Administrators");
        IQueryable<UserDbEntity> admins = _dbContext.Users.Where(user => user.IsBotAdmin);

        if (!admins.Any())
        {
            _ = embed.AddField("There currently zero administrators", $"User count: `{_dbContext.Users.Count()}`");
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

    /*
     * Blacklist Commands
     */

    [Command("blacklist"), RequireBotOwner]
    public class Blacklist
    {
        private readonly APKonsultContext _dbContext;

        public Blacklist(APKonsultContext context)
        {
            _dbContext = context;
        }

        [Command("user"),
            DefaultGroupCommand]
        public async ValueTask BlacklistMemberAsync(
            CommandContext ctx,

            DiscordUser user,

            [RemainingText]
            string? reason = null)
        {
            BlacklistedDbEntity? activeUser = _dbContext.Set<BlacklistedDbEntity>()
                .Where(bl => bl.UserId == user.Id)
                .FirstOrDefault();

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

        [Command("userid")]
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

        [Command("unblacklist"), RequireBotOwner]
        public async ValueTask UnblacklistMemberAsync(CommandContext ctx, DiscordUser user)
        {
            BlacklistedDbEntity? activeUser = _dbContext.Set<BlacklistedDbEntity>()
                .Where(bl => bl.UserId == user.Id)
                .FirstOrDefault();

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

        [Command("guild")]
        public async ValueTask BlacklistGuildAsync(CommandContext ctx, ulong guild_id, [RemainingText] string? reason = null)
        {
            DiscordGuild? guild = await ctx.Client.TryGetGuildAsync(guild_id);

            if (guild is null)
            {
                await ctx.RespondAsync("Failed to find a guild by that ID!");
                return;
            }

            BlacklistedDbEntity? activeGuild = _dbContext.Set<BlacklistedDbEntity>()
                .Where(bl => bl.GuildId == guild.Id)
                .FirstOrDefault();

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
            BlacklistedDbEntity? activeUser = _dbContext.Set<BlacklistedDbEntity>()
                .Where(bl => bl.GuildId == guild.Id)
                .FirstOrDefault();

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
}