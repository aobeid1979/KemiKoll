using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace KemiKoll.Domain.Hazards;

/// <summary>
/// Converts between <see cref="Pictogram"/> values and their official text codes (GHS01–GHS09).
/// </summary>
public static class PictogramCodes
{
    private const string Prefix = "GHS";

    /// <summary>Returns the official code, for example <c>GHS02</c> for <see cref="Pictogram.Flame"/>.</summary>
    /// <param name="pictogram">A defined pictogram.</param>
    /// <returns>The code in the format <c>GHS0n</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pictogram"/> is not one of the nine defined values.</exception>
    public static string ToCode(this Pictogram pictogram)
    {
        EnsureDefined(pictogram);
        return Prefix + ((int)pictogram).ToString("00", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses an official pictogram code such as <c>GHS02</c>.
    /// Case and surrounding spaces are ignored.
    /// </summary>
    /// <param name="code">The code to parse.</param>
    /// <param name="pictogram">The parsed pictogram, if successful.</param>
    /// <returns><see langword="true"/> if <paramref name="code"/> is one of GHS01–GHS09.</returns>
    public static bool TryParse([NotNullWhen(true)] string? code, out Pictogram pictogram)
    {
        pictogram = default;

        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var trimmed = code.Trim();

        // Exactly "GHS" followed by two digits, 01 to 09.
        if (trimmed.Length != Prefix.Length + 2
            || !trimmed.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            || trimmed[Prefix.Length] != '0'
            || trimmed[^1] is < '1' or > '9')
        {
            return false;
        }

        pictogram = (Pictogram)(trimmed[^1] - '0');
        return true;
    }

    /// <summary>Parses an official pictogram code such as <c>GHS02</c>.</summary>
    /// <param name="code">The code to parse.</param>
    /// <returns>The parsed pictogram.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException"><paramref name="code"/> is not one of GHS01–GHS09.</exception>
    public static Pictogram Parse(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return TryParse(code, out var pictogram)
            ? pictogram
            : throw new FormatException($"'{code}' is not a valid GHS pictogram code (expected GHS01 to GHS09).");
    }

    /// <summary>Throws if <paramref name="pictogram"/> is not one of the nine defined values.</summary>
    /// <param name="pictogram">The value to check.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is not defined, for example <c>(Pictogram)42</c>.</exception>
    public static void EnsureDefined(Pictogram pictogram)
    {
        // An enum variable can hold any integer, so a cast like (Pictogram)42 must be rejected explicitly.
        if (!Enum.IsDefined(pictogram))
        {
            throw new ArgumentOutOfRangeException(nameof(pictogram), pictogram, "Unknown GHS pictogram.");
        }
    }
}
