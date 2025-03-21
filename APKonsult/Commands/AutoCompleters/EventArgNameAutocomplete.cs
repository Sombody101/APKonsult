using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.Reflection;

namespace APKonsult.Commands.AutoCompleters;

internal class EventArgNameAutocomplete : IAutoCompleteProvider
{
    private static Dictionary<int, string> EventArgsForward = null!;
    private static Dictionary<string, int> EventArgsReverse = null!;

    public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        Initialize();

        bool getAll = string.IsNullOrWhiteSpace(context.UserInput);

        var result = EventArgsForward
            .Where(x => getAll || x.Value.Contains(context.UserInput ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .Select(v => new DiscordAutoCompleteChoice(v.Value, v.Value));

        DiscordAutoCompleteChoice[] l = [..result];

        return ValueTask.FromResult(result);
    }

    public static Dictionary<int, string> GetArgTypeNames()
    {
        var eventArgsNamespace = typeof(DiscordEventArgs).Namespace;
        var eventArgsAssembly = typeof(DiscordEventArgs).Assembly;

        var eventArgsTypes = eventArgsAssembly.GetTypes()
            .Where(type => type.Namespace == eventArgsNamespace)
            .ToList();

        var guildEventArgs = new Dictionary<int, string>();

        int ind = 0;
        foreach (var type in eventArgsTypes)
        {
            var guildField = type.GetProperty("Guild", BindingFlags.Instance | BindingFlags.Public);
            if (guildField is not null && guildField.PropertyType == typeof(DiscordGuild))
            {
                guildEventArgs.Add(ind++, type.Name);
            }
        }

        return guildEventArgs;
    }

    private void Initialize()
    {
        if (EventArgsForward is not null)
        {
            return;
        }

        EventArgsForward = GetArgTypeNames();
        EventArgsReverse = EventArgsForward
            .Select(kvp => new KeyValuePair<string, int>(kvp.Value, kvp.Key))
            .ToDictionary();
    }
}
