using APKonsult.Context;
using APKonsult.Models.Main;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace APKonsult.EventHandlers;

internal static partial class HandleTagEvent
{
    public static readonly Regex LocateTagRegex = TagRegex();

    public static async Task HandleTagAsync(DiscordClient client, MessageCreatedEventArgs args, APKonsultContext db)
    {
        // Check alias
        Match match = LocateTagRegex.Match(args.Message.Content);

        if (!match.Success)
        {
            return;
        }

        string tagName = match.Groups[1].Value;
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return;
        }

        tagName = tagName.Trim().ToLower();

        MessageTag? tag = await db.Set<MessageTag>().FirstOrDefaultAsync(tag => tag.Name == tagName && tag.UserId == args.Author.Id);

        if (tag is null)
        {
            return;
        }

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle("Tags not fully supported yet!")
            .WithAuthor(args.Author.Username)
            .WithDescription($"Here's your tag content for `{tag.Name}`!\n```txt\n{tag.Data}\n```");

        _ = await client.SendMessageAsync(args.Channel, embed);
    }

    [GeneratedRegex(@"\$(\S+)\b")]
    private static partial Regex TagRegex();
}