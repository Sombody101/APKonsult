using Humanizer;
using MoonSharp.Interpreter;
using Serilog;
using System.Reflection;

namespace APKonsult.Commands.Admin.TaskRunner.FunctionBindings;

public static class BindingsManager
{
    private const string BINDING_TARGET_NAMESPACE = "APKonsult.Commands.Admin.TaskRunner.FunctionBindings";

    public static Dictionary<string, object> CachedBindings { get; } = [];

    public static Dictionary<string, object> GetMethodBindings(bool forceReload = false)
    {
        if (forceReload)
        {
            CachedBindings.Clear();
        }

        if (CachedBindings.Count != 0)
        {
            return CachedBindings;
        }

        foreach ((MethodInfo method, LuaFunctionAttribute? luaName) in GetBindingMethods(Assembly.GetExecutingAssembly()))
        {
            if (luaName is null)
            {
                throw new LuaFunctionAttribute.InvalidLuaFunctionException($"No Lua name found for method '{method.Name}'.");
            }

            Type? delegateType = GetDelegateTypeForMethod(method);
            if (delegateType is not null)
            {
                try
                {
                    Delegate del = Delegate.CreateDelegate(delegateType, method);
                    CachedBindings.Add(luaName.FunctionName.Camelize(), del);
                }
                catch (ArgumentException ex)
                {
                    Log.Error(ex, "Error creating delegate for {Name}", method.Name);
                }
            }
            else
            {
                Log.Warning("Could not determine delegate type for {Name}", method.Name);
            }
        }

        return CachedBindings;
    }

    public static Table GetLuaConstants(Script script)
    {
        Table idTable = new(script);

        foreach ((string name, string id) in ChannelIDs.GetChannelIdConstants())
        {
            idTable[name] = id;
        }

        return idTable;
    }

    private static IEnumerable<(MethodInfo Method, LuaFunctionAttribute? Attribute)> GetBindingMethods(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t.Namespace == BINDING_TARGET_NAMESPACE)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Select(m => (Method: m, Attribute: m.GetCustomAttribute<LuaFunctionAttribute>()))
            .Where(tuple => tuple.Attribute is not null);
    }

    /// <summary>
    /// I genuinely hate this.
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    private static Type? GetDelegateTypeForMethod(MethodInfo method)
    {
        Type[] parameters = method.GetParameters().Select(p => p.ParameterType).ToArray();
        Type returnType = method.ReturnType;

        if (returnType == typeof(void))
        {
            switch (parameters.Length)
            {
                case 0: return typeof(Action);
                case 1: return typeof(Action<>).MakeGenericType(parameters);
                case 2: return typeof(Action<,>).MakeGenericType(parameters);
                case 3: return typeof(Action<,,>).MakeGenericType(parameters);
                case 4: return typeof(Action<,,,>).MakeGenericType(parameters);
                case 5: return typeof(Action<,,,,>).MakeGenericType(parameters);
                case 6: return typeof(Action<,,,,,>).MakeGenericType(parameters);
                case 7: return typeof(Action<,,,,,,>).MakeGenericType(parameters);
                case 8: return typeof(Action<,,,,,,,>).MakeGenericType(parameters);
            }
        }
        else
        {
            Type[] genericArgs = [.. parameters, returnType];
            switch (parameters.Length)
            {
                case 0: return typeof(Func<>).MakeGenericType(genericArgs);
                case 1: return typeof(Func<,>).MakeGenericType(genericArgs);
                case 2: return typeof(Func<,,>).MakeGenericType(genericArgs);
                case 3: return typeof(Func<,,,>).MakeGenericType(genericArgs);
                case 4: return typeof(Func<,,,,>).MakeGenericType(genericArgs);
                case 5: return typeof(Func<,,,,,>).MakeGenericType(genericArgs);
                case 6: return typeof(Func<,,,,,,>).MakeGenericType(genericArgs);
                case 7: return typeof(Func<,,,,,,,>).MakeGenericType(genericArgs);
                case 8: return typeof(Func<,,,,,,,,>).MakeGenericType(genericArgs);
            }
        }

        return null;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class LuaFunctionAttribute : Attribute
{
    public string FunctionName { get; }

    public LuaFunctionAttribute(string functionName)
    {
        FunctionName = functionName.Camelize();
    }

    public class InvalidLuaFunctionException(string message)
        : Exception(message)
    {
    }
}