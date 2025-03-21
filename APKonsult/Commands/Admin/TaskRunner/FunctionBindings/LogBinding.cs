using Serilog;

namespace APKonsult.Commands.Admin.TaskRunner.FunctionBindings;

internal static class LogBinding
{
    [LuaFunction("log")]
    public static void LogInformation(string input, params object[] objects)
    {
        Log.Information(Logify(input), objects);
    }

    [LuaFunction("logWarning")]
    public static void LogWarning(string input, params object[] objects)
    {
        Log.Warning(Logify(input), objects);
    }

    [LuaFunction("logError")]
    public static void LogError(string input, params object[] objects)
    {
        Log.Error(Logify(input), objects);
    }

    private static string Logify(string log)
    {
        return $"[LUASTATE]: {log}";
    }
}
