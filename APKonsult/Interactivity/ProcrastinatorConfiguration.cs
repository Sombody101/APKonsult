using APKonsult.Interactivity.Moments.Choose;
using APKonsult.Interactivity.Moments.Confirm;
using APKonsult.Interactivity.Moments.Pagination;
using APKonsult.Interactivity.Moments.Pick;
using APKonsult.Interactivity.Moments.Prompt;

namespace APKonsult.Interactivity;

public sealed record ProcrastinatorConfiguration
{
    public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public Dictionary<Type, IComponentCreator> ComponentCreators { get; init; } = new()
    {
        { typeof(IChooseComponentCreator), new ChooseDefaultComponentCreator() },
        { typeof(IConfirmComponentCreator), new ConfirmDefaultComponentCreator() },
        { typeof(IPaginationComponentCreator), new PaginationDefaultComponentCreator() },
        { typeof(IPickComponentCreator), new PickDefaultComponentCreator() },
        { typeof(IPromptComponentCreator), new PromptDefaultComponentCreator() }
    };

    public TComponentCreator GetComponentCreatorOrDefault<TComponentCreator, TDefaultComponentCreator>()
        where TComponentCreator : IComponentCreator
        where TDefaultComponentCreator : TComponentCreator, new()
    {
        return ComponentCreators.TryGetValue(typeof(TComponentCreator), out IComponentCreator? creator)
                    ? (TComponentCreator)creator
                    : new TDefaultComponentCreator();
    }
}
