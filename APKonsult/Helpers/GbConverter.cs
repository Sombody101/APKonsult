namespace APKonsult.Helpers;

public static class GBConverter
{
    public static string FormatSizeFromBytes(this long size)
    {
        return size switch
        {
            >= (1024 * 1024 * 1024) => $"{size / 1024f / 1024f / 1024f:0.00} GB",
            >= (1024 * 1024) => $"{size / 1024f / 1024f:n0} MB",
            >= 1024 => $"{size / 1024f:n0} KB",
            _ => $"{size} bytes"
        };
    }
}