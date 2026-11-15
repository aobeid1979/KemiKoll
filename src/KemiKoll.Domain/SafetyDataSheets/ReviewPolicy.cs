namespace KemiKoll.Domain.SafetyDataSheets;

/// <summary>
/// How old a safety data sheet may be before the company should check it
/// with the supplier for a newer version.
/// </summary>
/// <remarks>
/// REACH sets no fixed expiry date for a safety data sheet; the supplier must update it
/// when new information becomes available. The review age is therefore a company choice.
/// Each company can set its own policy; <see cref="Default"/> is used until it does.
/// </remarks>
public sealed record ReviewPolicy
{
    /// <summary>The shortest allowed review age, in years.</summary>
    public const int MinYears = 1;

    /// <summary>The longest allowed review age, in years.</summary>
    public const int MaxYears = 10;

    /// <summary>The review age used when a company has not chosen one: 3 years.</summary>
    public static ReviewPolicy Default { get; } = new(3);

    private ReviewPolicy(int maxAgeInYears) => MaxAgeInYears = maxAgeInYears;

    /// <summary>The number of years after its revision date that a data sheet is due for review.</summary>
    public int MaxAgeInYears { get; }

    /// <summary>
    /// Creates a review policy.
    /// </summary>
    /// <param name="maxAgeInYears">Review age in years, from <see cref="MinYears"/> to <see cref="MaxYears"/>.</param>
    /// <returns>The policy.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxAgeInYears"/> is outside the allowed range.</exception>
    public static ReviewPolicy Create(int maxAgeInYears)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAgeInYears, MinYears);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxAgeInYears, MaxYears);

        return new ReviewPolicy(maxAgeInYears);
    }
}
