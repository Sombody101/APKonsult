using APKonsult.Context;
using APKonsult.Models;
using APKonsult.Services.RegexServices;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text;

namespace APKonsult.EventHandlers;

public sealed class MessageCreatedEventHandler(APKonsultContext _dbContext, IRegexService _regexService) : IEventHandler<MessageCreatedEventArgs>
{
    public async Task HandleEventAsync(DiscordClient sender, MessageCreatedEventArgs eventArgs)
    {
        DiscordMessage message = eventArgs.Message;
        if (message.Author?.IsBot is null or true)
        {
            return;
        }

        UserDbEntity? user = await _dbContext.Users.FindAsync(message.Author.Id);
        if (user is null)
        {
            // User is not in DB. Add them and exit (there's no point continuing if there's no options to use)
            await _dbContext.FindOrCreateDbUserAsync(eventArgs.Author);
            return;
        }

        await HandleUserEmojiReactionAsync(sender, user, eventArgs);
        await HandleTagEvent.HandleTagAsync(sender, eventArgs, _dbContext);
        await HandleAfkStatusAsync(eventArgs, user);
    }

    private async Task HandleAfkStatusAsync(MessageCreatedEventArgs eventArgs, UserDbEntity user)
    {
        // Tracking service
        await _regexService.UseRegexAsync(eventArgs.Guild.Id, eventArgs.Channel.Id, eventArgs.Message);

        AfkStatusEntity? authorAfk = await _dbContext.Set<AfkStatusEntity>()
            .FirstOrDefaultAsync(x => x.UserId == eventArgs.Author.Id);

        if (authorAfk?.IsAfk() is true && eventArgs.Message.Content.Length > 5)
        {
            user.AfkStatus = null;
            _ = await _dbContext.SaveChangesAsync();
            _ = await eventArgs.Message.RespondAsync($"Welcome back {eventArgs.Author.Mention}!\nI've removed your AFK status.");
        }

        // Handle mentioned users
        if (eventArgs.MentionedUsers.Any())
        {
            IEnumerable<AfkStatusEntity> afkMentionedUsers = _dbContext.Set<AfkStatusEntity>()
                .Where(x => eventArgs.MentionedUsers.Select(u => u.Id).Contains(x.UserId) && x.IsAfk());

            if (afkMentionedUsers.Any())
            {
                StringBuilder sb = new();
                foreach (AfkStatusEntity? afkUser in afkMentionedUsers)
                {
                    _ = sb.AppendLine($"<@{afkUser.UserId}> went afk <t:{afkUser.AfkEpoch}:R>: {afkUser.AfkMessage ?? "[No message given]"}");
                }

                if (sb.Length > 0)
                {
                    _ = await eventArgs.Message.RespondAsync(sb.ToString());
                }
            }
        }
    }

    private static async Task HandleUserEmojiReactionAsync(DiscordClient client, UserDbEntity user, MessageCreatedEventArgs eventArgs)
    {
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

            await eventArgs.Message.CreateReactionAsync(emoji);
        }
        catch (Exception ex)
        {
            await ex.PrintExceptionAsync();
        }
    }
}
