using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Humanizer;

namespace APKonsult.Commands;

[Command("humanize")]
public static class HumanizerCommand
{
    [Command("text"), DefaultGroupCommand]
    public static async ValueTask HumanizeAsync(CommandContext ctx, [RemainingText] string text)
    {
        await ctx.RespondAsync(await HumanizeText(text));
    }

    [Command("title")]
    public static async ValueTask HumanizeTitleAsync(CommandContext ctx, [RemainingText] string text)
    {
        await ctx.RespondAsync(await HumanizeText(text, LetterCasing.Title));
    }

    [Command("caps")]
    public static async ValueTask HumanizeCapsAsync(CommandContext ctx, [RemainingText] string text)
    {
        await ctx.RespondAsync(await HumanizeText(text, LetterCasing.AllCaps));
    }

    [Command("lower"), TextAlias("low")]
    public static async ValueTask HumanizeLowerAsync(CommandContext ctx, [RemainingText] string text)
    {
        await ctx.RespondAsync(await HumanizeText(text, LetterCasing.LowerCase));
    }

    private static async ValueTask<DiscordEmbedBuilder> HumanizeText(string text, LetterCasing casing = LetterCasing.Sentence)
    {
        try
        {
            var embed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.SpringGreen)
                .AddField("Humanized Text", $"```\n{text.Humanize(casing)}\n```");

            return embed;
        }
        catch (Exception e)
        {
            await e.LogToWebhookAsync();

            return new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red)
                .AddField("Error while humanizing text!", e.Message);
        }
    }
}