using DSharpPlus.Commands.Processors.SlashCommands.Metadata;
using DSharpPlus.Entities;

namespace APKonsult.CommandChecks;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class UserGuildInstallableAttribute : InteractionInstallTypeAttribute
{
    public UserGuildInstallableAttribute()
        : base(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)
    {
    }
}