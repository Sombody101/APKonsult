using APKonsult.CommandChecks.Attributes;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using System.Globalization;

namespace APKonsult.Commands.Info;

public partial class InfoCommand
{
    /// <summary>
    /// Sends information about the provided role.
    /// </summary>
    /// <param name="role">Which role to get information about.</param>
    [Command("role"),
        RequireGuild,
        MadeBy(Creator.Lunar)]
    public static async Task RoleInfoAsync(CommandContext context, DiscordRole role)
    {
        DiscordEmbedBuilder embedBuilder = new()
        {
            Title = $"Role Info for {role.Name}",
            Author = new()
            {
                Name = context.Member!.DisplayName,
                IconUrl = context.User.AvatarUrl,
                Url = context.User.AvatarUrl
            },
            Color = role.Colors.PrimaryColor.Value == 0x000000
                ? Shared.DefaultEmbedColor
                : role.Colors.PrimaryColor
        };

        _ = embedBuilder.AddField("Color", role.Colors.PrimaryColor.ToString(), true);
        _ = embedBuilder.AddField("Created At", Formatter.Timestamp(role.CreationTimestamp.UtcDateTime, TimestampFormat.LongDateTime), true);
        _ = embedBuilder.AddField("Hoisted", role.IsHoisted.ToString(), true);
        _ = embedBuilder.AddField("Is Managed", role.IsManaged.ToString(), true);
        _ = embedBuilder.AddField("Is Mentionable", role.IsMentionable.ToString(), true);
        _ = embedBuilder.AddField("Role Id", Formatter.InlineCode(role.Id.ToString(CultureInfo.InvariantCulture)), true);
        _ = embedBuilder.AddField("Role Name", role.Name, true);
        _ = embedBuilder.AddField("Role Position", role.Position.ToString("N0", CultureInfo.InvariantCulture), true);
        _ = embedBuilder.AddField("Permissions", role.Permissions == DiscordPermissions.None
            ? "No permissions."
            : $"{role.Permissions}.", false);

        await context.RespondAsync(embedBuilder);
    }
}