using DSharpPlus.Commands.ContextChecks;

namespace APKonsult.CommandChecks.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireBotOwnerAttribute : ContextCheckAttribute
{
}