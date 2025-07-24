using APKonsult.CommandChecks.Attributes;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace APKonsult.Commands.Info;

public partial class InfoCommand
{
    [GeneratedRegex("<a?:(\\w+):(\\d+)>", RegexOptions.Compiled)]
    private static partial Regex _getEmojiRegex();

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_UnicodeEmojis")]
    private static extern Dictionary<string, string> _unicodeEmojis(DiscordEmoji emoji);

    /// <summary>
    /// Sends information about the provided emoji.
    /// </summary>
    /// <param name="emoji">The emoji to get information about. Unicode emojis are supported.</param>
    [Command("emoji"), MadeBy(Creator.Lunar)]
    public static async Task EmojiInfoAsync(CommandContext context, string emoji)
    {
        DiscordEmbedBuilder embedBuilder = new()
        {
            Color = new DiscordColor("#6b73db")
        };

        // We parse the emoji by hand in case if the bot doesn't have access to the emoji.
        if (DiscordEmoji.TryFromUnicode(context.Client, emoji, out DiscordEmoji? discordEmoji))
        {
            _ = embedBuilder.AddField("Emoji Name", _unicodeEmojis(null!).First(x => x.Value == discordEmoji.Name).Key.Replace(":", "\\:"), true);
            _ = embedBuilder.AddField("Unicode", $"\\{discordEmoji.Name}", true);
            embedBuilder.ImageUrl = $"https://raw.githubusercontent.com/twitter/twemoji/master/assets/72x72/{char.ConvertToUtf32(discordEmoji.Name, 0)
                .ToString("X4", CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture)}.png";
        }
        else if (DiscordEmoji.TryFromName(context.Client, emoji, out discordEmoji))
        {
            _ = embedBuilder.AddField("Emoji Name", discordEmoji.Name, true);
            _ = embedBuilder.AddField("Emoji ID", $"`{discordEmoji.Id.ToString(CultureInfo.InvariantCulture)}`", true);
            embedBuilder.ImageUrl = discordEmoji.Url;
        }
        else
        {
            Match match = _getEmojiRegex().Match(emoji);
            if (!match.Success)
            {
                await context.RespondAsync("Invalid emoji.");
                return;
            }

            _ = embedBuilder.AddField("Emoji Name", match.Groups[1].Value, true);
            _ = embedBuilder.AddField("Emoji ID", $"`{match.Groups[2].Value}`", true);
            embedBuilder.ImageUrl = $"https://cdn.discordapp.com/emojis/{match.Groups[2].Value}.png";
        }

        // ZWS field
        _ = embedBuilder.AddField("\u200B", "\u200B", true);
        _ = embedBuilder.AddField("Emoji URL", Formatter.MaskedUrl("Link to the image.", new Uri(embedBuilder.ImageUrl)), true);
        if (emoji.StartsWith("<a:", StringComparison.Ordinal))
        {
            _ = embedBuilder.AddField("GIF URL", Formatter.MaskedUrl("Link to the GIF.", new Uri(embedBuilder.ImageUrl)), true);
            embedBuilder.ImageUrl = embedBuilder.ImageUrl.Replace(".png", ".gif");
        }

        await context.RespondAsync(embedBuilder);
    }
}