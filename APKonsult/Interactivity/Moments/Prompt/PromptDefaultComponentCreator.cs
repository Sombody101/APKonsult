using DSharpPlus.Entities;

namespace APKonsult.Interactivity.Moments.Prompt;

public class PromptDefaultComponentCreator : IPromptComponentCreator
{
    public DiscordTextInputComponent CreateModalPromptButton(string question, string placeholder, Ulid id)
    {
        return new(question, id.ToString(), placeholder, required: true, style: DiscordTextInputStyle.Paragraph);
    }

    public DiscordButtonComponent CreateTextPromptButton(string question, Ulid id)
    {
        return new(DiscordButtonStyle.Primary, id.ToString(), "Click here to answer", false);
    }
}
