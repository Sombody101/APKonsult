using Humanizer;
using System.Runtime.CompilerServices;

namespace APKonsult;

public static class Qol
{
    /// <summary>
    /// Return <paramref name="string1"/> if <paramref name="go"/> is <see langword="true"/>, otherwise <paramref name="string2"/>
    /// </summary>
    /// <param name="string1"></param>
    /// <param name="string2"></param>
    /// <param name="go"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Pluralize(this string string1, string string2, bool go)
    {
        return go
            ? string1
            : string2;
    }

    /// <summary>
    /// Return <paramref name="text"/> if <paramref name="go"/> is <see langword="true"/>, otherwise <see cref="string.Empty"/>
    /// </summary>
    /// <param name="text"></param>
    /// <param name="go"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Pluralize(this string text, bool go)
    {
        return go
            ? text.Pluralize()
            : text;
    }

    /// <summary>
    /// Return <paramref name="plural"/> if <paramref name="go"/> is <see langword="true"/>, otherwise <see langword="'\0'"/>
    /// </summary>
    /// <param name="plural"></param>
    /// <param name="num"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char Pluralize(this char plural, bool go)
    {
        return go
            ? plural
            : '\u200B';
    }

    /// <summary>
    /// Return <paramref name="text"/> if <paramref name="num"/> is not equal to <see langword="1"/>, otherwise <see cref="string.Empty"/>
    /// </summary>
    /// <param name="text"></param>
    /// <param name="num"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Pluralize(this string text, int num)
    {
        return text.Pluralize(num is not 1);
    }

    /// <summary>
    /// Return <paramref name="plural"/> if <paramref name="num"/> is not <see langword="1"/>, otherwise <see langword="'\0'"/>
    /// </summary>
    /// <param name="plural"></param>
    /// <param name="num"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char Pluralize(this char plural, int num)
    {
        return plural.Pluralize(num is not 1);
    }
}