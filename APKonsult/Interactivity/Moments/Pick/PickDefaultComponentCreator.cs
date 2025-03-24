using DSharpPlus.Entities;

namespace APKonsult.Interactivity.Moments.Pick;

public class PickDefaultComponentCreator : IPickComponentCreator
{
    public DiscordSelectComponent CreatePickDropdown(string question, IReadOnlyList<string> options, Ulid id)
    {
        return new(
               id.ToString(), 
               "Answer here!", 
               options.Select(option => new DiscordSelectComponentOption(option, option)
        ));
    }
}
