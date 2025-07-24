namespace APKonsult.CommandChecks.Attributes;

/// <summary>
/// Specifies who made the command or the base code for it
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal class MadeByAttribute : Attribute
{
    public Creator Creator { get; init; }

    public MadeByAttribute(Creator creator)
    {
        Creator = creator;
    }

    public static readonly IReadOnlyDictionary<Creator, ulong> KnownCreators =
        new Dictionary<Creator, ulong>()
    {
        { Creator.Oke, Me },
        { Creator.Lunar, Lunar },
        { Creator.Plerx, Plerx },
    };

    public const ulong Lunar = 336733686529654798;
    public const ulong Plerx = 350967844957192192;
    public const ulong Velvet = 209279906280898562;
    public const ulong Me = 518296556059885599;
}

internal enum Creator
{
    Oke, // Me
    Lunar,
    Plerx
}