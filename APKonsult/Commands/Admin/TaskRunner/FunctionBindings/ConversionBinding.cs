using DSharpPlus.Entities;

namespace APKonsult.Commands.Admin.TaskRunner.FunctionBindings;

internal static class ConversionBinding
{
    [LuaFunction("guildId")]
    public static string GetIdFromGuild(DiscordGuild guild)
    {
        return IdToString(guild.Id);
    }

    [LuaFunction("channelId")]
    public static string GetIdFromChannel(DiscordChannel channel)
    {
        return IdToString(channel.Id);
    }

    [LuaFunction("roleId")]
    public static string GetIdFromRole(DiscordRole role)
    {
        return IdToString(role.Id);
    }

    [LuaFunction("userId")]
    public static string GetIdFromUser(DiscordUser user)
    {
        return IdToString(user.Id);
    }

    public static string IdToString(ulong id)
    {
        return $"id:{id}";
    }

    public static ulong StringToId(this string idStr)
    {
        if (idStr.StartsWith("id:"))
        {
            idStr = idStr[3..];
        }

        if (ulong.TryParse(idStr, out var id))
        {
            return id;
        }

        return 0;
    }
}
