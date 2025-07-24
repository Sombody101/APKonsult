using Humanizer;
using Serilog;
using System.Reflection;

namespace APKonsult;

internal static class ChannelIDs
{
    internal const string FILE_ROOT = "./0vol";

    private static Dictionary<string, string> cachedConstants = null!;

    public const ulong ABSOLUTE_ADMIN = 518296556059885599;

    public const ulong DEBUG_GUILD_ID = 1348833304811540490;
    public const ulong CHANNEL_DEBUG = 1348833305943867450;
    public const ulong CHANNEL_RELEASE = 1348833305943867451;

    public const ulong CHANNEL_GENERAL = 1348833305943867445;

    public const ulong BOT_TESTER_ROLE = 1348833304811540494;

    public static Dictionary<string, string> GetChannelIdConstants()
    {
        if (cachedConstants is not null)
        {
            return cachedConstants;
        }

        Dictionary<string, string> constants = [];

        FieldInfo[] fields = typeof(ChannelIDs).GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (FieldInfo field in fields)
        {
            if (field.FieldType != typeof(ulong))
            {
                Log.Warning("Channel ID field found of type '{FieldType}'.", field.FieldType);
                continue;
            }

            object value = field.GetValue(null)!;
            constants[field.Name.ToLower().Camelize()] = $"id:{value}";
        }

        cachedConstants = constants;
        return constants;
    }
}