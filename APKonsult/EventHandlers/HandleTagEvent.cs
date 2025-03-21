using APKonsult.Context;
using APKonsult.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace APKonsult.EventHandlers;

internal static partial class HandleTagEvent
{
    public static readonly Regex LocateTagRegex = TagRegex();

    public static async Task HandleTag(DiscordClient client, MessageCreatedEventArgs args, APKonsultContext db)
    {
        // Check alias
        var match = LocateTagRegex.Match(args.Message.Content);

        if (!match.Success)
            return;

        var tag_name = match.Groups[1].Value;
        if (string.IsNullOrWhiteSpace(tag_name))
            return;

        tag_name = tag_name.Trim().ToLower();

        var tag = await db.Set<MessageTag>().Where(tag => tag.Name == tag_name && tag.UserId == args.Author.Id)
            .FirstOrDefaultAsync();

        if (tag is null)
            return;

        var embed = new DiscordEmbedBuilder()
            .WithTitle("Tags not fully supported yet!")
            .WithAuthor(args.Author.Username)
            .WithDescription($"Here's your tag content for `{tag.Name}`!\n```txt\n{tag.Data}\n```");

        await client.SendMessageAsync(args.Channel, embed);
    }

    [GeneratedRegex(@"\$(\S+)\b")]
    private static partial Regex TagRegex();
}