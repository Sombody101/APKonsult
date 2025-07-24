using DSharpPlus.Entities;

namespace APKonsult.Interactivity.Moments.Pick;

public interface IPickComponentCreator : IComponentCreator
{
    public DiscordSelectComponent CreatePickDropdown(string question, IReadOnlyList<string> options, Ulid id);
}
