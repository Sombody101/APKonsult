using APKonsult.Context;
using APKonsult.Models;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;

namespace APKonsult.Commands.AutoCompleters;

internal class TrackerNameAutocomplete : IAutoCompleteProvider
{
    private readonly APKonsultContext _dbContext;

    public TrackerNameAutocomplete(APKonsultContext _db)
    {
        _dbContext = _db;
    }

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        return await _dbContext
                .Set<TrackingDbEntity>()
                .Where(x => x.GuildId == ctx.Guild.Id && x.Name.Contains(ctx.UserInput))
                .OrderBy(x => x.Name.IndexOf(ctx.UserInput))
                .Take(25)
                .ToDictionaryAsync(x => x.Name, x => (object)x.Name);
    }

    ValueTask<IEnumerable<DiscordAutoCompleteChoice>> IAutoCompleteProvider.AutoCompleteAsync(AutoCompleteContext context)
    {
        throw new NotImplementedException();
    }
}