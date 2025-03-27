using APKonsult.CommandChecks.Attributes;
using APKonsult.Context;
using APKonsult.Interactivity.Moments.Pagination;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Humanizer;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using Page = APKonsult.Interactivity.Moments.Pagination.Page;

namespace APKonsult.Commands;

public sealed partial class HelpCommand(APKonsultContext _dbContext)
{
    private const string NO_CODE_ARG = "--nocode";

    [Command("help"),
        Description($"Shows help information for commands. (Use `{NO_CODE_ARG}` to disable bot-tester information)")]
    public async ValueTask ExecuteAsync(
        CommandContext ctx,

        [Description("The parentCommand to get help information for."),
            RemainingText]
        string? command = null)
    {
        // Strip app information (only does anything if the user is a bot tester)
        bool noCode = command?.EndsWith(NO_CODE_ARG) is true;
        if (noCode)
        {
            command = command![..^NO_CODE_ARG.Length].TrimEnd();
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            await ctx.PaginateAsync(GetCommandPagesAsync(ctx, userIsAdmin: await IncludeAdminModules(ctx, _dbContext)));

            return;
        }
        else if (GetCommand(ctx.Extension.Commands.Values, command) is Command foundCommand)
        {
            if (foundCommand.Subcommands.Count > 0)
            {
                await ctx.PaginateAsync(GetCommandPagesAsync(ctx, foundCommand, await IncludeAdminModules(ctx, _dbContext)));
                return;
            }

            await ctx.RespondAsync(await GetCommandMessageAsync(ctx, foundCommand, noCode));
            return;
        }

        await ctx.RespondAsync($"Failed to find a Command by the name `{command}`.");
    }

    public static IEnumerable<Page> GetCommandPagesAsync(CommandContext context, Command? parentCommand = null, bool userIsAdmin = false)
    {
        List<Command> commands = (parentCommand?.Subcommands ?? context.Extension.Commands.Values)
            .Where(c => userIsAdmin || !c.Attributes.Any(a =>
            {
                Type attrType = a.GetType();
                return attrType == typeof(RequireAdminUserAttribute)
                    || attrType == typeof(RequireBotOwnerAttribute);
            }))
            .OrderBy(x => x.Name)
            .ToList();

        IEnumerable<IGrouping<string, Command>> groupedCommands = commands.GroupBy(c => c.Method?.DeclaringType?.Name ?? "Global");

        foreach (IGrouping<string, Command> group in groupedCommands)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithTitle(group.Key)
                .WithColor(DiscordColor.CornflowerBlue);

            foreach (Command? command in group)
            {
                _ = embed.AddField(command.Name.Titleize(), command.Description ?? "No description provided.");
            }

            DiscordMessageBuilder message = new DiscordMessageBuilder()
                .AddEmbed(embed);

            yield return new Page(message, description: embed.Fields.Select(x => x.Name).Humanize());
        }
    }

    private static async ValueTask<DiscordMessageBuilder> GetCommandMessageAsync(CommandContext context, Command command, bool noCode = false)
    {
        DiscordEmbedBuilder embed = new();

        string moduleType = command.GetType().IsClass
            ? "Module"
            : "Command";

        _ = embed.WithTitle($"Help {moduleType}: `{command.FullName.Titleize()}`");

        string commandCredit = command.Attributes.FirstOrDefault(attr => attr is MadeByAttribute) is not MadeByAttribute madeBy
            ? string.Empty
            : $"\n-# Created by `{context.Client.GetUserAsync(MadeByAttribute.KnownCreators[madeBy.Creator]).Result.Username}`";

        _ = embed.WithDescription($"{command.Description ?? "No description provided."}{commandCredit}");

        if (command.Subcommands.Count is not 0)
        {
            foreach (Command subCommand in command.Subcommands.OrderBy(x => x.Name))
            {
                string isDefaultCommand = subCommand.Attributes.Any(attr => attr is DefaultGroupCommandAttribute)
                    ? " ***[Default Command]***"
                    : string.Empty;

                _ = embed.AddField($"`{subCommand.FullName}`{isDefaultCommand}", subCommand.Description ?? "No description provided.");
            }
        }
        else
        {
            await EmbedCommandInformationAsync(context, command, embed, noCode);
        }

        return new DiscordMessageBuilder().AddEmbed(embed);
    }

    private static async ValueTask EmbedCommandInformationAsync(CommandContext context, Command command, DiscordEmbedBuilder embed, bool noCode)
    {
        if (command.Attributes.FirstOrDefault(x => x is RequirePermissionsAttribute) is RequirePermissionsAttribute permissions)
        {
            DiscordPermissions commonPermissions = permissions.BotPermissions & permissions.UserPermissions;
            DiscordPermissions botUniquePermissions = permissions.BotPermissions ^ commonPermissions;
            DiscordPermissions userUniquePermissions = permissions.UserPermissions ^ commonPermissions;
            StringBuilder builder = new();

            if (commonPermissions != default)
            {
                _ = builder.AppendLine(commonPermissions.ToString());
            }

            if (botUniquePermissions != default)
            {
                _ = builder.Append("**Bot**: ");
                _ = builder.AppendLine((permissions.BotPermissions ^ commonPermissions).ToString());
            }

            if (userUniquePermissions != default)
            {
                _ = builder.Append("**User**: ");
                _ = builder.AppendLine(permissions.UserPermissions.ToString());
            }

            _ = embed.AddField("Required Permissions", builder.ToString());
        }

        _ = embed.AddField("Usage", GetUsage(command));
        foreach (CommandParameter parameter in command.Parameters)
        {
            _ = embed.AddField($"{parameter.Name.Titleize()} - {context.Extension.GetProcessor<TextCommandProcessor>()
                .Converters[GetConverterFriendlyBaseType(parameter.Type)].ReadableName}", parameter.Description ?? "No description provided.");
        }

        MethodInfo? method = command.Method;

        // Check if user is me or has the bot tester role
        if (!noCode && method is not null && (context.User.IsOwner() || await context.User.IsBotTester()))
        {
            GetModuleInformation(embed, method);
        }

        _ = embed.WithFooter("<> = required, [] = optional")
            .MakeWide();
    }

    private static Command? GetCommand(IEnumerable<Command> commands, string name)
    {
        string commandName;
        int spaceIndex = -1;

        do
        {
            spaceIndex = name.IndexOf(' ', spaceIndex + 1);
            commandName = spaceIndex is -1
                ? name
                : name[..spaceIndex];

            commandName = commandName.Underscore();

            Command? foundCommand = commands.FirstOrDefault(cmd => cmd.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));

            if (foundCommand is not null)
            {
                if (spaceIndex is not -1)
                {
                    return GetCommand(foundCommand.Subcommands, name[(spaceIndex + 1)..]);
                }

                // // Check for default group parentCommand
                // if (foundCommand.Method is null) // Is class
                // {
                //     var found_sub_command = foundCommand.Subcommands.FirstOrDefault(sub =>
                //         sub.Method is not null
                //             && sub.Method.GetCustomAttributes(typeof(DefaultGroupCommandAttribute), false).Length is not 0
                //     );
                //
                //     if (found_sub_command is not null)
                //         return found_sub_command;
                // }

                return foundCommand;
            }

            // Search aliases
            foreach (Command command in commands)
            {
                foreach (Attribute attribute in command.Attributes)
                {
                    if (attribute is not TextAliasAttribute aliasAttribute)
                    {
                        continue;
                    }

                    if (Array.Exists(aliasAttribute.Aliases, alias => alias.Equals(commandName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return spaceIndex is -1
                            ? command
                            : GetCommand(command.Subcommands, name[(spaceIndex + 1)..]);
                    }
                }
            }

        } while (spaceIndex is not -1);

        return null;
    }

    // Good lord
    private static void GetModuleInformation(DiscordEmbedBuilder embed, MethodInfo method)
    {
        _ = embed.AddField("In Module",
            $"```ansi\n{Formatter.Colorize(method.DeclaringType?.FullName ?? "$UNKNOWN_MODULE", AnsiColor.Blue)}\n```");

        StringBuilder sb = new("\n");

        // Get method parameters
        foreach (ParameterInfo param in method.GetParameters())
        {
            // Get attributes for parameters (if any)
            IEnumerable<CustomAttributeData> attributes = param.CustomAttributes;
            if (attributes.Any())
            {
                _ = sb.Append('\n');

                foreach (Type? attr in attributes.Select(attr => attr.AttributeType))
                {
                    _ = sb.Append("\t[")
                        .Append(attr.Name[..^9]) // Remove 'Attribute' from the end of the string
                        .Append("]\n");
                }
            }

            _ = sb.Append('\t')
                .Append(param.ParameterType.IsPrimitive || param.ParameterType == typeof(string)
                    ? param.ParameterType.Name.ToLower()
                    : param.ParameterType.Name);

            _ = sb.Append(' ')
                .Append(param.Name)
                .Append(",\n");
        }

        string isStatic = method.IsStatic
                ? "static "
                : string.Empty;

        string accessor = method.IsPublic
            ? "public "
            : "private ";

        _ = embed.AddField("Method Declaration",
            $"```cs\n{accessor}{isStatic}async {method.ReturnType.Name} {method.Name}({sb.ToString().TrimEnd()[0..^1]}\n)\n```");

        // Get method attributes
        sb = new("```ansi\n");
        foreach (Type? attribute in method.CustomAttributes
            .Select(attr => attr.AttributeType)
            .Where(attr => !(attr.FullName?.StartsWith("System") ?? false)))
        {
            _ = sb.Append(Formatter.Colorize(attribute.Name[..^9].Humanize(LetterCasing.Title), AnsiColor.Magenta))
                .Append(",\n");
        }

        // In order for a method to show up here, there has to be at least one attribute (CommandAttribute), so no
        // need to check for the length of the string builder before taking a slice
        _ = embed.AddField("Attributes", $"{sb.ToString().Trim()[0..^1]}\n```");
    }

    private static string GetUsage(Command command)
    {
        StringBuilder builder = new();
        _ = builder.AppendLine("```ansi");
        _ = builder.Append('/');
        _ = builder.Append(Formatter.Colorize(command.FullName, AnsiColor.Cyan));

        foreach (CommandParameter parameter in command.Parameters)
        {
            if (!parameter.DefaultValue.HasValue)
            {
                _ = builder.Append(Formatter.Colorize(" <", AnsiColor.LightGray));
                _ = builder.Append(Formatter.Colorize(parameter.Name.Titleize(), AnsiColor.Magenta));
                _ = builder.Append(Formatter.Colorize(">", AnsiColor.LightGray));
            }
            else if (parameter.DefaultValue.Value != (parameter.Type.IsValueType
                ? Activator.CreateInstance(parameter.Type)
                : null))
            {
                _ = builder.Append(Formatter.Colorize(" [", AnsiColor.Yellow));
                _ = builder.Append(Formatter.Colorize(parameter.Name.Titleize(), AnsiColor.Magenta));
                _ = builder.Append(Formatter.Colorize($" = ", AnsiColor.LightGray));
                _ = builder.Append(Formatter.Colorize($"\"{parameter.DefaultValue.Value}\"", AnsiColor.Cyan));
                _ = builder.Append(Formatter.Colorize("]", AnsiColor.Yellow));
            }
            else
            {
                _ = builder.Append(Formatter.Colorize(" [", AnsiColor.Yellow));
                _ = builder.Append(Formatter.Colorize(parameter.Name.Titleize(), AnsiColor.Magenta));
                _ = builder.Append(Formatter.Colorize("]", AnsiColor.Yellow));
            }
        }

        _ = builder.Append("```");
        return builder.ToString();
    }

    private static int CountCommands(Command command)
    {
        int count = 0;
        if (command.Method is not null)
        {
            count++;
        }

        foreach (Command subcommand in command.Subcommands)
        {
            count += CountCommands(subcommand);
        }

        return count;
    }

    private static Type GetConverterFriendlyBaseType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.IsEnum)
        {
            return typeof(Enum);
        }
        else if (type.IsArray)
        {
            return type.GetElementType()!;
        }

        return Nullable.GetUnderlyingType(type) ?? type;
    }

    private static async ValueTask<bool> IncludeAdminModules(CommandContext ctx, APKonsultContext _dbContext)
    {
        return ctx.User.IsOwner() || await ctx.User.IsAdmin(_dbContext);
    }
}