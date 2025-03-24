using DSharpPlus.Entities;

namespace APKonsult.Interactivity.Moments.Confirm;

public interface IConfirmComponentCreator : IComponentCreator
{
    public DiscordButtonComponent CreateConfirmButton(string question, Ulid id, bool isYesButton);
}