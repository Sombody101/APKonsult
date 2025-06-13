namespace APKonsult.Exceptions;

public class TokenLoadException(string path) : Exception($"Failed to load token file: {path}")
{
}
