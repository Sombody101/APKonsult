using APKonsult.Models;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Globalization;

namespace APKonsult.Commands.Info;

partial class InfoCommand
{
    [Command("user"), 
        TextAlias("member"),
        DisplayName("Info User"), 
        SlashCommandTypes(DiscordApplicationCommandType.SlashCommand, DiscordApplicationCommandType.UserContextMenu)]
    public async Task UserInfoAsync(CommandContext context, DiscordUser? user = null)
    {
        user ??= context.User;

        var dbUser = await _dbContext.Set<UserDbEntity>().FirstOrDefaultAsync(u => u.Id == user.Id);

        DiscordEmbedBuilder embedBuilder = new()
        {
            Color = new DiscordColor(0x6b73db),
            Title = $"Info about {user.GetDisplayName()}",
            Thumbnail = new()
            {
                Url = user.AvatarUrl
            }
        };

        embedBuilder.AddField("User Id", Formatter.InlineCode(user.Id.ToString(CultureInfo.InvariantCulture)), true);
        embedBuilder.AddField("User Mention", user.Mention, true);

        List<string> userFlags = [];
        if (!user.Flags.HasValue || user.Flags.Value == DiscordUserFlags.None)
        {
            userFlags.Add("None");
        }
        else
        {
            for (int i = 0; i < (sizeof(DiscordUserFlags) * 8); i++)
            {
                DiscordUserFlags flag = (DiscordUserFlags)(1 << i);
                if (!user.Flags.Value.HasFlag(flag))
                {
                    continue;
                }

                // If the flag isn't documented, Humanize will return an empty string.
                // When that happens, we'll use the flag bit instead.
                string displayFlag = flag.Humanize().ToLower();
                if (string.IsNullOrWhiteSpace(displayFlag))
                {
                    // For whatever reason, the spammer flag is intentionally
                    // undocumented as "bots will never have a use for it".
                    displayFlag = i == 20 
                        ? "Likely spammer" 
                        : $"1 << {i}";
                }

                // Capitalize the first letter of the first flag.
                if (userFlags.Count == 0)
                {
                    displayFlag = char.ToUpper(displayFlag[0]) + displayFlag[1..];
                }

                userFlags.Add(displayFlag);
            }
        }

        embedBuilder.AddField("User Flags", $"{userFlags.DefaultIfEmpty($"Unknown flags: {user.Flags}").Humanize()}.", false);
        embedBuilder.AddField("Joined Discord", Formatter.Timestamp(user.CreationTimestamp, TimestampFormat.RelativeTime), true);

        // This means they're not in the server and they've never joined previously.
        if (dbUser is null)
        {
            await context.RespondAsync(embedBuilder);
            return;
        }

        // The user probably wasn't in the cache. Let's try to get them from the guild.
        if (user is not DiscordMember member)
        {
            try
            {
                member = await context.Guild!.GetMemberAsync(user.Id);
            }
            // The user is not in the guild.
            catch (DiscordException)
            {
                await context.RespondAsync(embedBuilder);
                return;
            }
        }

        embedBuilder.AddField("Roles", member.Roles.Any() 
            ? string.Join('\n', member.Roles.OrderByDescending(role => role.Position).Select(role => $"- {role.Mention}")) 
            : "None", false);

        // If the user has a color, set it.
        if (!member.Color.Equals(default(DiscordColor)))
        {
            embedBuilder.Color = member.Color.PrimaryColor;
        }

        await context.RespondAsync(embedBuilder);
    }
}