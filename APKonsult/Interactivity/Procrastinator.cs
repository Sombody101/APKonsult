using APKonsult.Interactivity.Moments.Idle;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace APKonsult.Interactivity;

public sealed partial class Procrastinator : IEventHandler<InteractionCreatedEventArgs>
{
    public ProcrastinatorConfiguration Configuration { get; init; }
    public IReadOnlyDictionary<Ulid, IdleMoment> Data => _data;

    private readonly Dictionary<Ulid, IdleMoment> _data = [];
    private readonly ILogger<Procrastinator> _logger;

    public Procrastinator(ProcrastinatorConfiguration? configuration = null, ILogger<Procrastinator>? logger = null)
    {
        Configuration = configuration ?? new();
        _logger = logger ?? NullLogger<Procrastinator>.Instance;
    }

    public bool TryAddData(Ulid id, IdleMoment data)
    {
        return _data.TryAdd(id, data);
    }

    public void UpdateTimeout(Ulid id, TimeSpan timeout)
    {
        if (_data.TryGetValue(id, out IdleMoment? data))
        {
            data.CancellationToken = RegisterTimeoutCallback(id, new CancellationTokenSource(timeout).Token);
        }
    }

    public CancellationToken RegisterTimeoutCallback(Ulid id, CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default)
        {
            cancellationToken = new CancellationTokenSource(Configuration.DefaultTimeout).Token;
        }

        _ = cancellationToken.Register(CancellationTokenCallbackAsync, id);
        return cancellationToken;
    }

    private async void CancellationTokenCallbackAsync(object? obj, CancellationToken cancellationToken)
    {
        try
        {
            if (obj is Ulid id && _data.TryGetValue(id, out IdleMoment? data)
                && data.CancellationToken == cancellationToken
                && _data.Remove(id))
            {
                await data.TimedOutAsync(this);
            }
        }
        catch (Exception error)
        {
            _logger.LogError(error, "An error occurred while handling a timed out procrastinator component.");
        }
    }

    public async Task HandleEventAsync(DiscordClient sender, InteractionCreatedEventArgs eventArgs)
    {
        if (eventArgs.Interaction.Data.CustomId?.Length >= 25
            && Ulid.TryParse(eventArgs.Interaction.Data.CustomId[..26], out Ulid id)
            && _data.TryGetValue(id, out IdleMoment? data)
            && !data.CancellationToken.IsCancellationRequested
        )
        {
            if (data.AuthorId != eventArgs.Interaction.User.Id)
            {
                await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You did not create this interaction and cannot modify it!")
                        .AsEphemeral());

                return;
            }

            if (_data.Remove(id))
            {
                await data.HandleAsync(this, eventArgs.Interaction);
            }
        }
    }
}
