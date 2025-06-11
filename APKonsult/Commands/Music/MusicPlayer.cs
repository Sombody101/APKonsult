using APKonsult.CommandChecks.Attributes;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;
using System.ComponentModel;

namespace APKonsult.Commands.Music;

internal sealed class MusicPlayer(IAudioService _audioService)
{
    [Command("join"),
        DebugOnly,
        DirectMessageUsage(DirectMessageUsage.DenyDMs)]
    public async Task JoinVoiceCallAsync(CommandContext ctx)
    {
        if (ctx.User is not DiscordMember member)
        {
            await ctx.RespondAsync("Failed to join voice call. Try again later.");
            return;
        }

        if (member.VoiceState.ChannelId is ulong)
        {
            await ctx.RespondAsync("You must be in a voice channel in order to use this command!");
            return;
        }

        // ctx.Client.call
    }

    [Command("play"),
        DebugOnly,
        Description("Plays music"),
        DirectMessageUsage(DirectMessageUsage.DenyDMs)]
    public async Task PlayTrackAsync(CommandContext context,

        [Parameter("query")]
        [Description("Track to play")]
        string query)
    {
        // This operation could take a while - deferring the interaction lets Discord know we've
        // received it and lets us update it later. Users see a "thinking..." state.
        await context.DeferResponseAsync().ConfigureAwait(false);

        // Attempt to get the player
        QueuedLavalinkPlayer? player = await GetPlayerAsync(context, connectToVoiceChannel: true).ConfigureAwait(false);

        // If something went wrong getting the player, don't attempt to play any tracks
        if (player is null)
        {
            return;
        }

        // Fetch the tracks
        Lavalink4NET.Tracks.LavalinkTrack? track = await _audioService.Tracks
            .LoadTrackAsync(query, TrackSearchMode.YouTube)
            .ConfigureAwait(false);

        // If no results were found
        if (track is null)
        {
            DiscordFollowupMessageBuilder errorResponse = new DiscordFollowupMessageBuilder()
                .WithContent("😖 No results.")
                .AsEphemeral();

            _ = await context
                .EditResponseAsync(errorResponse)
                .ConfigureAwait(false);

            return;
        }

        // Play the track
        int position = await player
            .PlayAsync(track)
            .ConfigureAwait(false);

        // If it was added to the queue
        if (position is 0)
        {
            _ = await context
                .FollowupAsync(new DiscordFollowupMessageBuilder().WithContent($"🔈 Playing: {track.Uri}"))
                .ConfigureAwait(false);
        }

        // If it was played directly
        else
        {
            _ = await context
                .FollowupAsync(new DiscordFollowupMessageBuilder().WithContent($"🔈 Added to queue: {track.Uri}"))
                .ConfigureAwait(false);
        }
    }

    private async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(CommandContext ctx, bool connectToVoiceChannel = true)
    {
        PlayerRetrieveOptions retrieveOptions = new(ChannelBehavior: connectToVoiceChannel
            ? PlayerChannelBehavior.Join
            : PlayerChannelBehavior.None);

        QueuedLavalinkPlayerOptions playerOptions = new() { HistoryCapacity = 10000 };

        PlayerResult<QueuedLavalinkPlayer> result = await _audioService.Players
            .RetrieveAsync(ctx.Guild!.Id, ctx.Member!.VoiceState.ChannelId, playerFactory: PlayerFactory.Queued, Options.Create(playerOptions), retrieveOptions)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return result.Player;
        }

        string errorMessage = result.Status switch
        {
            PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
            PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
            _ => "Unknown error.",
        };

        DiscordFollowupMessageBuilder errorResponse = new DiscordFollowupMessageBuilder()
            .WithContent(errorMessage)
            .AsEphemeral();

        _ = await ctx
            .FollowupAsync(errorResponse)
            .ConfigureAwait(false);

        return null;
    }
}
