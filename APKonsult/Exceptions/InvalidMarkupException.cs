namespace APKonsult.Exceptions;

public class InvalidMarkupException : Exception
{
    public InvalidMarkupException(string tagName, int index)
        : base($"Invalid {tagName} at index {index}")
    { }
}