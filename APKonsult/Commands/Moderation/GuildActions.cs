using APKonsult.CommandChecks.Attributes;
using APKonsult.Context;
using APKonsult.Exceptions;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using Humanizer;
using System.Text;
using Handler = System.Func<DSharpPlus.Commands.CommandContext, string>;

namespace APKonsult.Commands.Moderation;

[RequirePermissions(DiscordPermission.Administrator)]
public class GuildActions
{
    private readonly APKonsultContext _dbContext;

    public GuildActions(APKonsultContext _db)
    {
        _dbContext = _db;
    }

    [Command("set"), DebugOnly]
    public async ValueTask SetGuildWelcomeMessageAsync(CommandContext ctx, [RemainingText] string message)
    {
        string? parsedMessage = await new Markup(message).ApplyMarkupAsync(ctx);

        if (parsedMessage is null)
        {
            return;
        }

        await ctx.RespondAsync(new DiscordEmbedBuilder()
            .WithTitle("Final output")
            .WithColor(DiscordColor.SpringGreen)
            .WithDescription(parsedMessage));
    }

    [Command("test")]
    public static async ValueTask TestTagsAsync(CommandContext ctx)
    {
        await ctx.RespondAsync(new DiscordEmbedBuilder()
            .WithTitle("Tag Examples")
            .WithColor(DiscordColor.Cyan)
            .WithDescription(Markup.TestTags(ctx)));
    }

    private sealed class Markup
    {
        private const char Null = '\0';

        private readonly string text;
        private int index = 0;

        public Markup(string _text)
        {
            text = _text;
        }

        public async ValueTask<string?> ApplyMarkupAsync(CommandContext ctx)
        {
            StringBuilder output = new();
            StringBuilder tagBuffer = new();
            List<string> errors = new();

            bool collectingTag = false;
            char c;
            while (peek() is not Null)
            {
                c = consume();

                if (c is '{')
                {
                    if (peek() is '{') // Escaped tag
                    {
                        _ = output.Append(c)
                            .Append(consume());
                        continue;
                    }

                    collectingTag = true;
                }
                else if (c is '}')
                {
                    if (peek() is '}')
                    {
                        if (collectingTag)
                        {
                            errors.Add($"1. Invalid escaped closing tag at index {index + 1}");
                            continue;
                        }

                        _ = output.Append(c)
                            .Append(consume());

                        continue;
                    }

                    collectingTag = false;

                    try
                    {
                        string? resolvedTag = ResolveTag(ctx, tagBuffer.ToString());
                        _ = output.Append(resolvedTag);
                    }
                    catch (InvalidTagException tagErr)
                    {
                        errors.Add($"1. {tagErr.Message} at index {index - tagBuffer.Length}");
                    }
                    finally
                    {
                        _ = tagBuffer.Clear();
                    }
                }
                else if (collectingTag)
                {
                    _ = tagBuffer.Append(c);
                }
                else
                {
                    _ = output.Append(c);
                }
            }

            if (errors.Count is not 0)
            {
                DiscordEmbedBuilder errorEmbed = new DiscordEmbedBuilder()
                    .WithTitle("Markup Error!")
                    .WithColor(DiscordColor.Red)
                    .WithDescription(errors.Count > 50
                        ? $"More than {errors.Count}+ errors found.\nPlease follow proper markup syntax."
                        : string.Join('\n', errors));

                await ctx.RespondAsync(errorEmbed);
                return null;
            }

            return output.ToString();
        }

        public static string TestTags(CommandContext ctx)
        {
            StringBuilder output = new();

            foreach (string? key in markupTags.Select(kvp => kvp.Key))
            {
                _ = output.AppendLine($"1. `{key}`: {ResolveTag(ctx, key)}");
            }

            return output.ToString();
        }

        private char peek(int skip = 1)
        {
            return index + skip > text.Length ? Null : text[index];
        }

        private char consume()
        {
            return text[index++];
        }

        private static string? ResolveTag(CommandContext ctx, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new InvalidTagException();
            }

            object? result = markupTags.FirstOrDefault(x => x.Key == tag).Value;

            if (result is null)
            {
                goto UnknownTag;
            }

            if (result is Handler handler)
            {
                return handler(ctx);
            }
            else if (result is string stringResult)
            {
                return stringResult;
            }

        UnknownTag:
            throw new InvalidTagException(tag);
        }

        private static readonly IReadOnlyDictionary<string, object> markupTags
            = new Dictionary<string, object>
        {
               { "newline", "\n" },

               // User information
               { "user", new Handler((ctx) => ctx.User.Mention) },
               { "user_name", new Handler((ctx) => ctx.User.Username) },
               { "user_avatar", new Handler((ctx) => ctx.User.AvatarUrl) },
               { "user_id", new Handler((ctx) => ctx.User.Id.ToString()) },
               { "user_nick", new Handler((ctx) => ctx.User is DiscordMember member
                    ? member.Nickname
                    : ctx.User.Username) },

                { "user_joindate", new Handler((ctx) => ctx.User is DiscordMember member
                    ? member.JoinedAt.Humanize()
                    : "unknown join date") },

                { "user_createdate", new Handler((ctx) => ctx.User is DiscordMember member
                    ? member.CreationTimestamp.Humanize()
                    : "unknown join date") },

               // Guild information
               { "server_name", new Handler((ctx) => ctx.Guild.Name) },
               { "server_id", new Handler((ctx) => ctx.Guild.Id.ToString()) },
               { "server_membercount", new Handler((ctx) => ctx.Guild.MemberCount.ToString()) },
               { "server_icon", new Handler((ctx) => ctx.Guild.IconUrl) },
               { "server_rolecount", new Handler((ctx) => ctx.Guild.Roles.Count.ToString()) },
               { "server_owner", new Handler((ctx) => ctx.Guild.GetGuildOwnerAsync().Result.Mention) },
               { "server_owner_id", new Handler((ctx) => ctx.Guild.GetGuildOwnerAsync().Result.Id.ToString()) },
               { "server_createdate", new Handler((ctx) => ctx.Guild.CreationTimestamp.Humanize()) },
        };
    }
}