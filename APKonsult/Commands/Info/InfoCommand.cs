using APKonsult.CommandChecks;
using APKonsult.CommandChecks.Attributes;
using APKonsult.Context;
using APKonsult.Services;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.Metadata;
using DSharpPlus.Entities;

namespace APKonsult.Commands.Info;

/// <summary>
/// Creates a new instance of <see cref="InfoCommand"/>.
/// </summary>
/// <param name="imageUtilitiesService">Required service for fetching image metadata.</param>
/// <param name="allocationRateTracker">Required service for tracking the memory allocation rate.</param>
[Command("info"),
    RequirePermissions([DiscordPermission.EmbedLinks], []),
    UserGuildInstallable,
    InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel),
    MadeBy(Creator.Lunar)]
public sealed partial class InfoCommand(AllocationRateTracker allocationRateTracker, APKonsultContext _dbContext)
{
}