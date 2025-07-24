using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace APKonsult.Interactivity.Moments.Confirm;

public static class ExtensionMethods
{
    public static async ValueTask<bool?> ConfirmAsync(this DiscordMember member,
        Procrastinator procrastinator,
        string question, IConfirmComponentCreator? componentCreator = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(procrastinator);
        ArgumentNullException.ThrowIfNull(question);
        componentCreator ??= procrastinator.Configuration.GetComponentCreatorOrDefault<IConfirmComponentCreator, ConfirmDefaultComponentCreator>();

        Ulid id = Ulid.NewUlid();
        ConfirmMoment data = new()
        {
            Id = id,
            AuthorId = member.Id,
            ComponentCreator = componentCreator,
            CancellationToken = procrastinator.RegisterTimeoutCallback(id, cancellationToken),
            Question = question
        };

        List<DiscordButtonComponent> buttons = [componentCreator.CreateConfirmButton(question, id, true), 
            componentCreator.CreateConfirmButton(question, id, false)];
        for (int i = 0; i < buttons.Count; i++)
        {
            DiscordButtonComponent button = buttons[i];
            if (!button.CustomId.StartsWith(id.ToString(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"The custom id of the button must start with the id of the data. Index: {i}");
            }
        }

        if (!procrastinator.TryAddData(id, data))
        {
            throw new InvalidOperationException("The data could not be added to the dictionary.");
        }

        data.Message = await member.SendMessageAsync(new DiscordMessageBuilder()
            .WithAllowedMentions(Mentions.None)
            .WithContent(question)
            .AddActionRowComponent(buttons)
        );

        _ = await data.TaskCompletionSource.Task;
        return data.TaskCompletionSource.Task.Result;
    }

    public static async ValueTask<bool?> ConfirmAsync(this CommandContext context,
        string question,
        IConfirmComponentCreator? componentCreator = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(question);

        Procrastinator procrastinator = context.ServiceProvider.GetRequiredService<Procrastinator>();
        componentCreator ??= procrastinator.Configuration.GetComponentCreatorOrDefault<IConfirmComponentCreator, ConfirmDefaultComponentCreator>();

        Ulid id = Ulid.NewUlid();
        ConfirmMoment data = new()
        {
            Id = id,
            AuthorId = context.User.Id,
            ComponentCreator = componentCreator,
            CancellationToken = procrastinator.RegisterTimeoutCallback(id, cancellationToken),
            Question = question
        };

        List<DiscordButtonComponent> buttons = [componentCreator.CreateConfirmButton(question, id, true), 
            componentCreator.CreateConfirmButton(question, id, false)];

        for (int i = 0; i < buttons.Count; i++)
        {
            DiscordButtonComponent button = buttons[i];
            if (!button.CustomId.StartsWith(id.ToString(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"The custom id of the button must start with the id of the data. Index: {i}");
            }
        }

        if (!procrastinator.TryAddData(id, data))
        {
            throw new InvalidOperationException("The data could not be added to the dictionary.");
        }

        await context.RespondAsync(new DiscordMessageBuilder()
            .WithAllowedMentions(Mentions.None)
            .WithContent(question)
            .AddActionRowComponent(buttons)
        );

        data.Message = await context.GetResponseAsync();
        _ = await data.TaskCompletionSource.Task;
        return data.TaskCompletionSource.Task.Result;
    }
}