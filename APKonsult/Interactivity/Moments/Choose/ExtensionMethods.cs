using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace APKonsult.Interactivity.Moments.Choose;

public static class ChooseExtensions
{
    public static async ValueTask<IReadOnlyList<string>> ChooseAsync(this DiscordMember member, Procrastinator procrastinator, string question, IReadOnlyList<string> options, IChooseComponentCreator? componentCreator = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(procrastinator);
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(options);

        componentCreator ??= procrastinator.Configuration.GetComponentCreatorOrDefault<IChooseComponentCreator, ChooseDefaultComponentCreator>();

        Ulid id = Ulid.NewUlid();
        ChooseMoment data = new()
        {
            Id = id,
            AuthorId = member.Id,
            CancellationToken = procrastinator.RegisterTimeoutCallback(id, cancellationToken),
            ComponentCreator = componentCreator,
            Question = question,
            Options = options
        };

        DiscordSelectComponent dropDown = componentCreator.CreateChooseDropdown(question, options, id);
        if (!dropDown.CustomId.StartsWith(id.ToString(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The custom id of the select menu must start with the id of the data.");
        }
        else if (!procrastinator.TryAddData(id, data))
        {
            throw new InvalidOperationException("The data could not be added to the dictionary.");
        }

        data.Message = await member.SendMessageAsync(new DiscordMessageBuilder()
            .WithAllowedMentions(Mentions.None)
            .WithContent(question)
            .AddActionRowComponent(dropDown)
        );

        _ = await data.TaskCompletionSource.Task;
        return data.TaskCompletionSource.Task.Result;
    }

    public static async ValueTask<IReadOnlyList<string>> ChooseAsync(this CommandContext context, string question, IReadOnlyList<string> options, IChooseComponentCreator? componentCreator = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(options);

        Procrastinator procrastinator = context.ServiceProvider.GetRequiredService<Procrastinator>();
        componentCreator ??= procrastinator.Configuration.GetComponentCreatorOrDefault<IChooseComponentCreator, ChooseDefaultComponentCreator>();

        Ulid id = Ulid.NewUlid();
        ChooseMoment data = new()
        {
            Id = id,
            AuthorId = context.User.Id,
            CancellationToken = procrastinator.RegisterTimeoutCallback(id, cancellationToken),
            ComponentCreator = componentCreator,
            Question = question,
            Options = options
        };

        DiscordSelectComponent dropDown = componentCreator.CreateChooseDropdown(question, options, id);
        if (!dropDown.CustomId.StartsWith(id.ToString(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The custom id of the select menu must start with the id of the data.");
        }
        else if (!procrastinator.TryAddData(id, data))
        {
            throw new InvalidOperationException("The data could not be added to the dictionary.");
        }

        await context.RespondAsync(new DiscordMessageBuilder()
            .WithAllowedMentions(Mentions.None)
            .WithContent(question)
            .AddActionRowComponent(dropDown)
        );

        data.Message = await context.GetResponseAsync();
        _ = await data.TaskCompletionSource.Task;
        return data.TaskCompletionSource.Task.Result;
    }
}