using DSharpPlus.Entities;

namespace APKonsult.Interactivity.Moments.Choose;

public class ChooseDefaultComponentCreator : IChooseComponentCreator
{
    public DiscordSelectComponent CreateChooseDropdown(string question, IReadOnlyList<string> options, Ulid id)
    {
        return new(id.ToString(), "Answer here!",
            options.Select(option => new DiscordSelectComponentOption(option, option)),
             false,
             1,
            options.Count
        );
    }
}