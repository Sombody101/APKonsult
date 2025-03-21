namespace APKonsult.Exceptions;

public class InvalidTagException : Exception
{
    public InvalidTagException(string tag)
        : base($"Unknown tag `{tag}`")
    { }

    public InvalidTagException()
        : base("Empty tag")
    { }
}