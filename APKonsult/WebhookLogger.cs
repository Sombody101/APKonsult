using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Entities;

namespace APKonsult;

public static class WebhookLogger
{
    /// <summary>
    /// Turns the given <see cref="Exception"/> <paramref name="exception"/> into a formatted <see cref="DiscordEmbedBuilder"/>.
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    public static IEnumerable<DiscordEmbedBuilder> MakeEmbedFromException(this Exception exception)
    {
        return FromException(exception);
    }

    private static IEnumerable<DiscordEmbedBuilder> FromException(Exception ex)
    {
        return ex switch
        {
            ChecksFailedException cfex => cfex.Errors.Select(f => f.Exception)
                .Where(e => e is not null)
                .SelectMany(FromException!),

            _ => CreateEmbeds(ex),
        };
    }

    private static List<DiscordEmbedBuilder> CreateEmbeds(Exception exception)
    {
        Exception? ex = exception;
        List<DiscordEmbedBuilder> embeds = [];

        do
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithTitle($"Bot Exception [From {Program.BUILD_TYPE} Build]")
                .WithColor(DiscordColor.Red)
                .AddField("Exception Type", ex.GetType().Name)
                .AddField("Exception Source", ex.Source ?? "[no exception source]")
                .AddField("Base", ex.TargetSite?.Name ?? "[no base method]")
                .WithFooter($"Uptime: {Shared.FormatTickCount()}")
                .MakeWide();

            string description = ex.Message;

            if (ex.StackTrace?.Length < 4096 - 13 - description.Length)
            {
                description = $"{description}\n```less\n{ex.StackTrace}\n```";
                embed.WithDescription(description);
                embeds.Add(embed);
            }
            else
            {
                embeds.Add(embed);
                embeds.AddRange(GetEmbedStackTrace(ex));
            }
        } while ((ex = ex?.InnerException) is not null);

        return embeds;
    }

    private static IEnumerable<DiscordEmbedBuilder> GetEmbedStackTrace(Exception ex)
    {
        string? trace = ex.StackTrace;
        if (string.IsNullOrWhiteSpace(trace))
        {
            yield break;
        }

        const int MAX_CHARS = 4096 - 12;
        int count = (int)Math.Ceiling(trace.Length / (float)MAX_CHARS);
        for (int i = 0; i < count; ++i)
        {
            int start = i * MAX_CHARS;
            int length = start + Math.Min(trace.Length - start, MAX_CHARS);
            string description = $"```less\n{trace[start..length]}\n```";

            yield return new DiscordEmbedBuilder()
                .WithTitle($"{i + 1}/{count}")
                .WithDescription(description)
                .WithColor(DiscordColor.Red);
        }
    }
}
