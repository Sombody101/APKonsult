using APKonsult.Commands.Admin.TaskRunner.FunctionBindings;
using APKonsult.Models;
using APKonsult.Services;
using DSharpPlus.EventArgs;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using Serilog;
using System.Reflection;

namespace APKonsult.Commands.Admin.TaskRunner;

public sealed class TaskRuntime
{
    private const string CACHE_CALLBACK = "eventFired";

    private Script luaScript = null!;

    public string TextScript { get; }

    public bool Active { get; private set; }

    public EventAction Action { get; private set; } = null!;

    public uint InvokeCount { get; private set; } = 0;

    public TaskRuntime(string script)
    {
        UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;

        Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.String, typeof(ulong),
            v =>
            {
                string value = (string)v.ToObject();
                return value.StartsWith("id:")
                    ? value[3..].StringToId()
                    : null;
            }
        );

        TextScript = script;
        InitializeLuaScript();
    }

    public int ExecuteScript(EventAction action, DiscordEventArgs? args)
    {
        Action = action;

        try
        {
            if (args is not null)
            {
                _ = UserData.RegisterType(args.GetType());
                UpdateEventArgs(args);
            }

            DynValue result = luaScript.DoString(TextScript);

            InvokeCount++;

            return (int)result.Number;
        }
        catch (ScriptRuntimeException sEx)
        {
            Log.Error(sEx, "Lua Runtime exception failed for {ActionName}, for guild {GuildId}", Action.ActionName, Action.GuildId);
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Lua task script failed.");
            throw new LuaTaskFailedException(ex.Message, ex);
        }
    }

    public int VisitCallback(DiscordEventArgs args, string functionName = CACHE_CALLBACK, bool noThrow = false)
    {
        DynValue function = luaScript.Globals.RawGet(functionName);

        if (function == DynValue.Nil)
        {
            if (!noThrow)
            {
                throw new EventFiredCallbackMissingException(functionName);
            }

            string scriptName = Action?.ActionName ?? "[no action set]";
            Log.Error("No event handler method found in Lua script '{ScriptName}'", scriptName);
            return 127;
        }

        try
        {
            UpdateEventArgs(args);

            DynValue result = luaScript.Call(function);

            InvokeCount++;

            return (int)result.Number;
        }
        catch (ScriptRuntimeException sEx)
        {
            Log.Error(sEx, "Lua Runtime exception failed for callback {ActionName}, for guild {GuildId}", Action.ActionName, Action.GuildId);
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Lua task callback failed.");
            throw new LuaTaskFailedException(ex.Message, ex);
        }
    }

    private void UpdateEventArgs(DiscordEventArgs args)
    {
        DynValue luaEventArgs = UserData.Create(args);
        luaScript.Globals.Set("eventArgs", luaEventArgs);
    }

    private void InitializeLuaScript()
    {
        luaScript = new(CoreModules.Preset_HardSandbox);
        luaScript.Options.DebugPrint = (line) => Log.Debug("[DEBUG LUASTATE]: {Line}", line);
        luaScript.Options.CheckThreadAccess = false;
        luaScript.Options.ScriptLoader = null;

        _ = luaScript.Globals.RegisterConstants();
        luaScript.Globals["ids"] = BindingsManager.GetLuaConstants(luaScript);
        luaScript.Globals["client"] = DiscordClientService.StaticInstance!.Client;
        luaScript.Globals["action"] = Action;

        foreach ((string name, object method) in BindingsManager.GetMethodBindings())
        {
            luaScript.Globals[name] = method;
        }

        luaScript.Globals["keepAlive"] = () =>
        {
            if (Action is null)
            {
                throw new InvalidLuaRuntimeCallbackException();
            }

            Log.Information("({ActionName}, {EventName}) for {GuildName} has been preserved.", Action.ActionName, Action.EventName, Action.Guild.Name);
            Active = true;
        };

        luaScript.Globals["await"] = (Func<DynValue, DynValue>)(value =>
        {
            return AwaitTaskAsync(value).Result;
        });
    }

    private async Task<DynValue> AwaitTaskAsync(DynValue taskDynValue)
    {
        if (taskDynValue.Type is not DataType.UserData)
        {
            throw new ScriptRuntimeException($"Expected a Task object, got a {taskDynValue.Type}");
        }

        try
        {
            object taskObject = taskDynValue.ToObject();

            if (taskObject is not Task task)
            {
                throw new ScriptRuntimeException("Passed value is not a Task.");
            }

            await task;

            if (!task.GetType().IsGenericType)
            {
                return DynValue.Nil;
            }

            PropertyInfo? resultProperty = task.GetType().GetProperty("Result");

            if (resultProperty is null)
            {
                return DynValue.Nil;
            }

            object? resultValue = resultProperty.GetValue(task);

            return resultValue is not null
                ? DynValue.FromObject(luaScript, resultValue)
                : DynValue.Nil;
        }
        catch (Exception ex)
        {
            throw new ScriptRuntimeException($"Error awaiting task: {ex.Message}");
        }
    }

    public class LuaTaskFailedException : Exception
    {
        public LuaTaskFailedException(string message)
            : base(message)
        {
        }

        public LuaTaskFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class InvalidLuaRuntimeCallbackException : Exception
    {
        public InvalidLuaRuntimeCallbackException()
            : base("Cannot keep Lua state active because no EventAction was passed.")
        {
        }
    }

    public class EventFiredCallbackMissingException(string callbackName)
        : Exception($"The Lua event function '{callbackName}' was not found.")
    {
    }
}
