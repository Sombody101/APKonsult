using APKonsult.Services;
using DSharpPlus;
using DSharpPlus.Entities;

namespace APKonsult.Commands.Admin.TaskRunner.FunctionBindings;

internal static class ContextBindings
{
    [LuaFunction(nameof(GetGuild))]
    public static DiscordGuild? GetGuild(string name, DiscordClient? client = null)
    {
        client ??= DiscordClientService.GetClient();

        return client.Guilds
            .Select(pair => pair.Value)
            .FirstOrDefault(g => g.Name.EndsWith(name, StringComparison.OrdinalIgnoreCase));
    }

    [LuaFunction(nameof(GetChannel))]
    public static DiscordChannel? GetChannel(DiscordGuild guild, string name)
    {
        return guild.Channels
            .Select(pair => pair.Value)
            .FirstOrDefault(c => c.Name.EndsWith(name, StringComparison.OrdinalIgnoreCase));
    }

    [LuaFunction(nameof(GetRole))]
    public static DiscordRole? GetRole(DiscordGuild guild, string name)
    {
        return guild.Roles
            .Select(pair => pair.Value)
            .FirstOrDefault(r => r.Name.EndsWith(name, StringComparison.OrdinalIgnoreCase));
    }
}
