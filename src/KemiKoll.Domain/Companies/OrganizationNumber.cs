using System.Diagnostics.CodeAnalysis;

namespace KemiKoll.Domain.Companies;

/// <summary>
/// A Swedish organisation number (organisationsnummer), for example <c>556036-0793</c>.
/// </summary>
/// <remarks>
/// <para>
/// The number has ten digits; the last one is a check digit calculated with the
/// Luhn algorithm, the same as for Swedish personal identity numbers.
/// </para>
/// <para>
/// A sole proprietorship (enskild firma) uses the owner's personal identity number
/// as its organisation number. Those numbers are accepted too, because small
/// sole-trader workshops are an important customer group. Such a number is personal
/// data under GDPR and must be handled accordingly.
/// </para>
/// </remarks>
public sealed record OrganizationNumber
{
    private const int DigitCount = 10;

    private OrganizationNumber(string value) => Value = value;

    /// <summary>The number in the standard format <c>NNNNNN-NNNN</c>.</summary>
    public string Value { get; }

    /// <summary>
    /// Tries to parse an organisation number written as <c>NNNNNN-NNNN</c> or <c>NNNNNNNNNN</c>.
    /// Surrounding spaces are ignored.
    /// </summary>
    /// <param name="input">The text to parse.</param>
    /// <param name="number">The parsed number, or <see langword="null"/> if it is not valid.</param>
    /// <returns><see langword="true"/> if the format and the check digit are valid.</returns>
    public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out OrganizationNumber? number)
    {
        number = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();

        // Accept the hyphen only in its standard position (after six digits).
        var digits = trimmed.Length == DigitCount + 1 && trimmed[6] == '-'
            ? string.Concat(trimmed.AsSpan(0, 6), trimmed.AsSpan(7))
            : trimmed;

        if (digits.Length != DigitCount || !digits.All(char.IsAsciiDigit) || !HasValidCheckDigit(digits))
        {
            return false;
        }

        number = new OrganizationNumber($"{digits[..6]}-{digits[6..]}");
        return true;
    }

    /// <summary>Parses an organisation number.</summary>
    /// <param name="input">The text to parse.</param>
    /// <returns>The parsed number.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The format or the check digit is not valid.</exception>
    public static OrganizationNumber Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return TryParse(input, out var number)
            ? number
            : throw new FormatException($"'{input}' is not a valid Swedish organisation number (expected NNNNNN-NNNN).");
    }

    /// <summary>Returns the number in the format <c>NNNNNN-NNNN</c>.</summary>
    public override string ToString() => Value;

    // Luhn algorithm: from the left, every other digit (starting with the first) is doubled;
    // if the result is above 9, subtract 9. The sum of all ten digits must end in 0.
    private static bool HasValidCheckDigit(string digits)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            var value = (digits[i] - '0') * (i % 2 == 0 ? 2 : 1);
            sum += value > 9 ? value - 9 : value;
        }

        return sum % 10 == 0;
    }
}
