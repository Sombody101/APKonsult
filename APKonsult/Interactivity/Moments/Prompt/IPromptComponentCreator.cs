using DSharpPlus.Entities;

namespace APKonsult.Interactivity.Moments.Prompt;

public interface IPromptComponentCreator : IComponentCreator
{
    public DiscordButtonComponent CreateTextPromptButton(string question, Ulid id);
    public DiscordTextInputComponent CreateModalPromptButton(string question, string placeholder, Ulid id);
}
