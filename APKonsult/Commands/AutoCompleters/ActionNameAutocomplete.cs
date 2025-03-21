using APKonsult.Context;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;

namespace APKonsult.Commands.AutoCompleters;

internal class ActionNameAutocomplete(APKonsultContext _dbContext) : IAutoCompleteProvider
{
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        var guild = await _dbContext.Guilds
            .Include(x => x.DefinedActions)
            .FirstOrDefaultAsync(x => x.Id == context.Guild.Id);

        if (guild is null)
        {
            return [];
        }

        var actions = guild.DefinedActions
            .Where(x => x.ActionName.Contains(context.UserInput, StringComparison.OrdinalIgnoreCase));

        if (!actions.Any())
        {
            return [];
        }

        return actions
            .Take(25)
            .Select(x => new DiscordAutoCompleteChoice(x.ActionName, x.ActionName));
    }
}
