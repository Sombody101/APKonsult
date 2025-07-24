using DSharpPlus.Entities;

namespace APKonsult.Interactivity.Moments.Confirm;

public class ConfirmDefaultComponentCreator : IConfirmComponentCreator
{
    public DiscordButtonComponent CreateConfirmButton(string question, Ulid id, bool isYesButton)
    {
        return new(
             isYesButton
                ? DiscordButtonStyle.Success
                : DiscordButtonStyle.Danger,
             $"{id}_{isYesButton.ToString().ToLowerInvariant()}",
             isYesButton
                ? "Yes"
                : "No",
             false
        );
    }
}