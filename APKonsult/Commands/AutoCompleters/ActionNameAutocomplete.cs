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
        Models.GuildDbEntity? guild = await _dbContext.Guilds
            .Include(x => x.DefinedActions)
            .FirstOrDefaultAsync(x => x.Id == context.Guild.Id);

        if (guild is null)
        {
            return [];
        }

        IEnumerable<Models.EventAction> actions = guild.DefinedActions
            .Where(x => x.ActionName.Contains(context.UserInput, StringComparison.OrdinalIgnoreCase));

        return !actions.Any()
            ? ([])
            : actions
            .Take(25)
            .Select(x => new DiscordAutoCompleteChoice(x.ActionName, x.ActionName));
    }
}
