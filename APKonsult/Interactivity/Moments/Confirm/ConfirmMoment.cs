using APKonsult.Interactivity.Moments.Idle;
using DSharpPlus.Entities;

namespace APKonsult.Interactivity.Moments.Confirm;

public record ConfirmMoment : IdleMoment<IConfirmComponentCreator>
{
    public required string Question { get; init; }
    public TaskCompletionSource<bool?> TaskCompletionSource { get; init; } = new();

    public override async ValueTask HandleAsync(Procrastinator procrastinator, DiscordInteraction interaction)
    {
        if (interaction.Type != DiscordInteractionType.Component
            || interaction.Message?.Components is null
            || interaction.Message.Components.Count == 0)
        {
            return;
        }

        DiscordInteractionResponseBuilder responseBuilder = new(new DiscordMessageBuilder(interaction.Message));
        responseBuilder.ClearComponents();
        responseBuilder.AddActionRowComponent(interaction.Message.Components.Mutate<DiscordButtonComponent>(
            button => button.CustomId.StartsWith(Id.ToString(), StringComparison.Ordinal) && !button.Disabled,
            button =>
            {
                if (button.CustomId == interaction.Data.CustomId)
                {
                    TaskCompletionSource.SetResult(button.CustomId == ComponentCreator.CreateConfirmButton(Question, Id, true).CustomId);
                }

                return button.Disable();
            }
        ).Cast<DiscordButtonComponent>());

        await interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, responseBuilder);
    }

    public override ValueTask TimedOutAsync(Procrastinator procrastinator)
    {
        TaskCompletionSource.SetResult(null);
        return base.TimedOutAsync(procrastinator);
    }
}