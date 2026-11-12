using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace KemiKoll.Domain.Hazards;

/// <summary>
/// A hazard statement code (H-phrase) from section 2 of a safety data sheet,
/// for example <c>H225</c>, <c>H360FD</c> or <c>EUH066</c>.
/// </summary>
/// <remarks>
/// This is a value object: two statements with the same code are equal.
/// It only stores the code. The wording ("Highly flammable liquid and vapour")
/// is taken from the safety data sheet itself, because the text differs per language.
/// An instance can only be created through <see cref="Parse"/> or <see cref="TryParse"/>,
/// so every <see cref="HazardStatement"/> in the system has a valid code.
/// </remarks>
public sealed partial record HazardStatement
{
    private HazardStatement(string code, HazardCategory category)
    {
        Code = code;
        Category = category;
    }

    /// <summary>The normalized code, for example <c>H225</c> or <c>EUH066</c>.</summary>
    public string Code { get; }

    /// <summary>The hazard group the code belongs to.</summary>
    public HazardCategory Category { get; }

    /// <summary>
    /// Parses a hazard statement code.
    /// </summary>
    /// <param name="code">The code as written on the safety data sheet, for example <c>"H225"</c>.</param>
    /// <returns>The parsed hazard statement.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException"><paramref name="code"/> is not a valid hazard statement code.</exception>
    public static HazardStatement Parse(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return TryParse(code, out var statement)
            ? statement
            : throw new FormatException($"'{code}' is not a valid hazard statement code (expected for example H225 or EUH066).");
    }

    /// <summary>
    /// Tries to parse a hazard statement code without throwing.
    /// </summary>
    /// <param name="code">The code as written on the safety data sheet.</param>
    /// <param name="statement">The parsed statement, or <see langword="null"/> if the code is not valid.</param>
    /// <returns><see langword="true"/> if <paramref name="code"/> is a valid hazard statement code.</returns>
    public static bool TryParse([NotNullWhen(true)] string? code, [NotNullWhen(true)] out HazardStatement? statement)
    {
        statement = null;

        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var match = CodePattern().Match(code.Trim());
        if (!match.Success)
        {
            return false;
        }

        // Only the prefix is normalized to upper case. The suffix letters of
        // reproductive toxicity codes are case-sensitive in CLP:
        // capital F/D = "may damage", small f/d = "suspected of damaging",
        // so H360FD and H360Fd are different statements.
        var number = match.Groups["number"].Value;
        var suffix = match.Groups["suffix"].Value;

        if (match.Groups["euh"].Success)
        {
            statement = new HazardStatement($"EUH{number}{suffix.ToUpperInvariant()}", HazardCategory.SupplementalEu);
            return true;
        }

        var category = number[0] switch
        {
            '2' => HazardCategory.Physical,
            '3' => HazardCategory.Health,
            _ => HazardCategory.Environmental,
        };

        statement = new HazardStatement($"H{number}{suffix}", category);
        return true;
    }

    /// <summary>Returns the code, for example <c>H225</c>.</summary>
    public override string ToString() => Code;

    // H codes:   H + 200–499 + optional suffix of up to two letters from F, D, f, d, i (e.g. H360FD, H361d, H350i).
    // EUH codes: EUH + three digits + optional A (e.g. EUH066, EUH201A).
    [GeneratedRegex(
        @"^(?:[Hh](?<number>[234]\d{2})(?<suffix>[FDfdi]{0,2})|(?<euh>[Ee][Uu][Hh])(?<number>\d{3})(?<suffix>[Aa]?))$",
        RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
