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
        var embedBuilder = new DiscordEmbedBuilder()
        {
            Title = $"Role Info for {role.Name}",
            Author = new()
            {
                Name = context.Member!.DisplayName,
                IconUrl = context.User.AvatarUrl,
                Url = context.User.AvatarUrl
            },
            Color = role.Color.Value == 0x000000
                ? Shared.DefaultEmbedColor
                : role.Color
        };

        embedBuilder.AddField("Color", role.Color.ToString(), true);
        embedBuilder.AddField("Created At", Formatter.Timestamp(role.CreationTimestamp.UtcDateTime, TimestampFormat.LongDateTime), true);
        embedBuilder.AddField("Hoisted", role.IsHoisted.ToString(), true);
        embedBuilder.AddField("Is Managed", role.IsManaged.ToString(), true);
        embedBuilder.AddField("Is Mentionable", role.IsMentionable.ToString(), true);
        embedBuilder.AddField("Role Id", Formatter.InlineCode(role.Id.ToString(CultureInfo.InvariantCulture)), true);
        embedBuilder.AddField("Role Name", role.Name, true);
        embedBuilder.AddField("Role Position", role.Position.ToString("N0", CultureInfo.InvariantCulture), true);
        embedBuilder.AddField("Permissions", role.Permissions == DiscordPermissions.None
            ? "No permissions."
            : role.Permissions.ToString() + ".", false);

        await context.RespondAsync(embedBuilder);
    }
}