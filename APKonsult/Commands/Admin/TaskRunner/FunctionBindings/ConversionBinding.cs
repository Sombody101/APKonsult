namespace APKonsult.Commands.Admin.TaskRunner.FunctionBindings;

internal static class ConversionBinding
{
    [LuaFunction("id")]
    public static ulong StringToId(this string idStr)
    {
        if (ulong.TryParse(idStr, out var id))
        {
            return id;
        }

        return 0;
    }
}
