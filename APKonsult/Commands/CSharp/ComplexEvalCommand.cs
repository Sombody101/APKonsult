using APKonsult.CommandChecks;
using APKonsult.CommandChecks.Attributes;
using APKonsult.Configuration;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.Metadata;
using DSharpPlus.Entities;
using Humanizer;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Formatter = DSharpPlus.Formatter;

namespace APKonsult.Commands.CSharp;

public static partial class ComplexEvalCommand
{
    private const int MaxFormattedFieldSize = 1000;
    private const int MaxFieldNameLength = 256;

    private static readonly HttpClient _httpClient = new();

    public class Result
    {
        public object ReturnValue { get; set; } = null!;
        public string Exception { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string ExceptionType { get; set; } = string.Empty;
        public TimeSpan ExecutionTime { get; set; }
        public TimeSpan CompileTime { get; set; }
        public string ConsoleOut { get; set; } = string.Empty;
        public string ReturnTypeName { get; set; } = string.Empty;
    }

    [Command("cs"),
        RequireAdminUser,
        UserGuildInstallable,
        InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel)]
    public static async Task EvaluateCSharpAsync(CommandContext ctx, string code)
    {
        if (ctx.Channel is null)
        {
            await ModifyOrSendErrorEmbedAsync("The REPL can only be executed in public guild channels.", ctx);
            return;
        }

        DiscordMessage message = await ctx.Channel
            .SendMessageAsync(embed: new DiscordEmbedBuilder()
                .WithTitle("REPL Executing")
                .WithAuthor(ctx.User.Username)
                .WithColor(DiscordColor.Orange)
                .WithDescription($"Compiling and Executing...")
                .Build());

        StringContent content = new(code, Encoding.UTF8, "text/plain");

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(ConfigManager.Manager.BotConfig.ReplUrl, content);

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.BadRequest)
            {
                await ModifyOrSendErrorEmbedAsync($"Status Code: {(int)response.StatusCode} {response.StatusCode}", ctx, message);
                return;
            }

            Result? replResponse = JsonConvert.DeserializeObject<Result>(await response.Content.ReadAsStringAsync());

            if (replResponse is null)
            {
                await ModifyOrSendErrorEmbedAsync("Failed to deserialize the REPL result from JSON to a Result!", ctx, message);
                return;
            }

            DiscordEmbedBuilder embed = BuildEmbed(ctx.User, replResponse);

            _ = await message.ModifyAsync(msg =>
            {
                msg.Content = null;
                _ = msg.ClearEmbeds();
                _ = msg.AddEmbed(embed);
            });
        }
        catch (HttpRequestException ex)
        {
            await ctx.RespondAsync($"Error communicating with REPL server: {ex.Message}");
        }
    }

    private static async Task ModifyOrSendErrorEmbedAsync(string error, CommandContext ctx, DiscordMessage? message = null)
    {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle("REPL Error")
            .WithAuthor(ctx.User.Username)
            .WithColor(DiscordColor.Red)
            .WithDescription(error);

        if (message is null)
        {
            await ctx.RespondAsync(embed);
        }
        else
        {
            _ = await message.ModifyAsync(msg =>
            {
                msg.Content = null;
                _ = msg.ClearEmbeds();
                _ = msg.AddEmbed(embed);
            });
        }
    }

    private static DiscordEmbedBuilder BuildEmbed(DiscordUser guildUser, Result parsedResult)
    {
        string returnValue = parsedResult.ReturnValue?.ToString() ?? " ";
        string consoleOut = parsedResult.ConsoleOut;
        bool hasException = !string.IsNullOrEmpty(parsedResult.Exception);
        string status = hasException ? "Failure" : "Success";

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithTitle($"REPL Result: {status}")
                .WithColor(hasException ? DiscordColor.Red : DiscordColor.Green)
                .WithAuthor(guildUser.Username)
                .WithFooter($"Compile: {parsedResult.CompileTime.TotalMilliseconds:F}ms | Execution: {parsedResult.ExecutionTime.TotalMilliseconds:F}ms");

        _ = embed.WithDescription(FormatOrEmptyCodeblock(parsedResult.Code, "cs"));

        if (parsedResult.ReturnValue is not null)
        {
            _ = embed.AddField($"Result: {parsedResult.ReturnTypeName}".Truncate(MaxFormattedFieldSize),
                FormatOrEmptyCodeblock(returnValue.Truncate(MaxFormattedFieldSize), "json"));
        }

        if (!string.IsNullOrWhiteSpace(consoleOut))
        {
            _ = embed.AddField("Console Output", Formatter.BlockCode(consoleOut.Truncate(MaxFormattedFieldSize), "txt"));
        }

        if (hasException)
        {
            string diffFormatted = DiffGenerationRegex().Replace(parsedResult.Exception, "- ");
            _ = embed.AddField($"Exception: {parsedResult.ExceptionType}".Truncate(MaxFieldNameLength),
                Formatter.BlockCode(diffFormatted.Truncate(MaxFormattedFieldSize), "diff"));
        }

        return embed;
    }

    private static string FormatOrEmptyCodeblock(string input, string language)
    {
        return string.IsNullOrWhiteSpace(input)
            ? "```\n```"
            : Formatter.BlockCode(input, language);
    }

    [GeneratedRegex("^", RegexOptions.Multiline)]
    private static partial Regex DiffGenerationRegex();
}