﻿using APKonsult.Commands;
using APKonsult.Context;
using APKonsult.Services;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace APKonsult;

public static class Shared
{
#if DEBUG
    // Only used in debug builds for the '!restart' command
    public const string PREVIOUS_INSTANCE_ARG = "from_previous_instance";
#endif

    // Runtime constants
    public static readonly DiscordColor DefaultEmbedColor = new(0xFFE4B5);

    public static string GenerateModalId()
    {
        return $"modal-{Random.Shared.Next():X4}";
    }

    /// <summary>
    /// Checks if the given <paramref name="user"/> is me.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public static bool IsOwner(this DiscordUser user)
    {
        return user.Id == ChannelIDs.ABSOLUTE_ADMIN;
    }

    /// <summary>
    /// Checks if the given <paramref name="user"/> is an active bot administrator. 
    /// </summary>
    /// <param name="user"></param>
    /// <param name="_dbContext"></param>
    /// <returns></returns>
    public static async Task<bool> IsAdminAsync(this DiscordUser user, APKonsultContext _dbContext)
    {
        Models.UserDbEntity? dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.IsBotAdmin && u.Id == user.Id);

        return dbUser is not null;
    }

    /// <summary>
    /// Checks if the given <paramref name="user"/> is currently in the <see cref="ChannelIDs.DEBUG_GUILD_ID"/> guild.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public static async Task<bool> IsBotTesterAsync(this DiscordUser user)
    {
        if (DiscordClientService.StaticInstance?.Client.Guilds.TryGetValue(ChannelIDs.DEBUG_GUILD_ID, out DiscordGuild? debugGuild) is not true)
        {
            Log.Warning("Failed to find debug guild for user bot-tester check!");
            return false;
        }

        try
        {
            DiscordMember member = await debugGuild.GetMemberAsync(user.Id);
            return member.Roles.Any(role => role.Id == ChannelIDs.BOT_TESTER_ROLE);
        }
        catch (DSharpPlus.Exceptions.NotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Turns the given <see cref="Exception"/> <paramref name="ex"/> into a formatted <see cref="DiscordEmbedBuilder"/>.
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    public static DiscordEmbedBuilder MakeEmbedFromException(this Exception ex)
    {
        string stackTrace = ex.StackTrace ?? "$NO_STACK_TRACE";

        if (stackTrace.Length > 700)
        {
            stackTrace = "$TRACE_TOO_LARGE";

            // Log the exception twice to make sure it will be seen.
            Log.Error(ex, "Exception was too large to place im embed: {Message}", ex.Message);
        }

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle($"Bot Exception [From {Program.BUILD_TYPE} Build]")
            .WithColor(DiscordColor.Red)
            .WithDescription(ex.Message)
            .AddField("Exception Type", ex.GetType().Name)
            .AddField("Exception Source", ex.Source ?? "$NO_EXCEPTION_SOURCE")
            .AddField("Base", ex.TargetSite?.Name ?? "$NO_BASE_METHOD")
            .AddField("Stack Trace", $"```less\n{stackTrace}\n```")
            .WithFooter($"Uptime: {PingCommand.FormatTickCount()}");

        return embed;
    }

    /// <summary>
    /// Adds a transparent image to the given <paramref name="embed"/> that makes the whole embed wider.
    /// </summary>
    /// <param name="embed"></param>
    /// <returns></returns>
    public static DiscordEmbedBuilder MakeWide(this DiscordEmbedBuilder embed)
    {
        if (embed.ImageUrl is null)
        {
            _ = embed.WithImageUrl("https://files.forsaken-borders.net/transparent.png");
        }

        return embed;
    }

    /// <summary>
    /// Adds the default bot color to a <see cref="DiscordEmbedBuilder"/>.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DiscordEmbedBuilder WithDefaultColor(this DiscordEmbedBuilder builder)
    {
        return builder.WithColor(DefaultEmbedColor);
    }

    /// <summary>
    /// Prints the given <see cref="Exception"/> <paramref name="ex"/> to the console, 
    /// then sends a Discord Webhook message with the passed <paramref name="sender"/>.
    /// </summary>
    /// <param name="ex"></param>
    /// <param name="sender"></param>
    /// <returns></returns>
    public static async Task PrintExceptionAsync(this Exception ex, [Optional] Type? sender)
    {
        Log.Error(ex, ex.Message);
        await ex.LogToWebhookAsync(sender);
    }

    /// <inheritdoc cref="LogToWebhookAsync(Exception, Type?)"/>
    public static async Task LogToWebhookAsync(this Exception ex, object sender)
    {
        await ex.LogToWebhookAsync(sender?.GetType() ?? typeof(void));
    }

    /// <summary>
    /// Sends a Discord Webhook message with then given <see cref="Exception"/> <paramref name="ex"/>, 
    /// and the passed <paramref name="sender"/>.
    /// </summary>
    /// <param name="ex"></param>
    /// <param name="sender"></param>
    /// <returns></returns>
    public static async Task LogToWebhookAsync(this Exception ex, [Optional] Type? sender)
    {
        if (Program.WebhookClient.Webhooks.Count is 0)
        {
            // Can't log anything
            return;
        }

        DiscordWebhookBuilder webhookBuilder = new DiscordWebhookBuilder()
            .WithUsername($"APKonsult-{Program.BUILD_TYPE}")
            .AddEmbed(ex.MakeEmbedFromException()
                .WithFooter($"From: {sender?.Name ?? "$NO_MODULE_PASSED"}\nUptime: {PingCommand.FormatTickCount()}"));

        _ = await Program.WebhookClient.BroadcastMessageAsync(webhookBuilder);
    }

    /// <summary>
    /// Attempts to create a new <see cref="APKonsultContext"/> from <see cref="DiscordClientService.DbContextFactory"/>.
    /// If it can't, <see langword="null"/> is returned.
    /// [Not used for commands]
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task<APKonsultContext?> TryGetDbContextAsync(CancellationToken token = default)
    {
        return DiscordClientService.DbContextFactory is null ? null : await DiscordClientService.DbContextFactory.CreateDbContextAsync(token);
    }

    /// <summary>
    /// Gets the given <paramref name="user"/>s Discord display name. 
    /// If it cannot be found, their username is returned instead.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public static string GetDisplayName(this DiscordUser user)
    {
        if (user is DiscordMember member)
        {
            return member.DisplayName;
        }
        else if (!string.IsNullOrEmpty(user.GlobalName))
        {
            return user.GlobalName;
        }
        else if (user.Discriminator is "0")
        {
            return user.Username;
        }

        return $"{user.Username}#{user.Discriminator}";
    }

    public static async Task<DiscordGuild?> TryGetGuildAsync(this DiscordClient client, ulong id)
    {
        try
        {
            return await client.GetGuildAsync(id);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<DiscordUser?> TryGetUserAsync(this DiscordClient client, ulong id)
    {
        try
        {
            return await client.GetUserAsync(id);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Removes the surrounding markdown code block from a string. 
    /// If no code block is found, the original string is returned.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="expectedCodeType"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public static bool TryRemoveCodeBlock(string input, CodeType expectedCodeType, [NotNullWhen(true)] out string? code)
    {
        code = null;
        ReadOnlySpan<char> inputSpan = input.AsSpan();

        if (inputSpan.Length > 6
            && inputSpan.StartsWith("```")
            && inputSpan.EndsWith("```")
            && expectedCodeType.HasFlag(CodeType.Codeblock))
        {
            int index = inputSpan.IndexOf('\n');
            if (index == -1 || !FromCodeAttribute.CodeBlockLanguages.Contains(inputSpan[3..index].ToString()))
            {
                code = input[3..^3];
                return true;
            }

            code = input[(index + 1)..^3];
            return true;
        }
        else if (inputSpan.Length > 4
            && inputSpan.StartsWith("``")
            && inputSpan.EndsWith("``")
            && expectedCodeType.HasFlag(CodeType.Inline))
        {
            code = input[2..^2];
        }
        else if (inputSpan.Length > 2
            && inputSpan.StartsWith("`")
            && inputSpan.EndsWith("`")
            && expectedCodeType.HasFlag(CodeType.Inline))
        {
            code = input[1..^1];
        }

        return code is not null;
    }
}

public static class ChannelHelpers
{
    public static async Task<DiscordChannel> GetDmChannelAsync(this CommandContext ctx)
    {
        if (ctx.Channel.GuildId is null)
        {
            return ctx.Channel;
        }

        return await ctx.User.CreateDmChannelAsync();
    }
}