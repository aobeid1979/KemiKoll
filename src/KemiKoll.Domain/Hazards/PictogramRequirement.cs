namespace KemiKoll.Domain.Hazards;

/// <summary>
/// The pictograms required by a set of hazard statements, and how a chosen set compares to them.
/// </summary>
/// <param name="Required">The required pictograms, in official order (GHS01 … GHS09).</param>
/// <param name="CodesWithoutRule">
/// Hazard statements for which no pictogram rule is known; the user must check these by hand.
/// </param>
public sealed record PictogramRequirement(IReadOnlyList<Pictogram> Required, IReadOnlyList<string> CodesWithoutRule)
{
    /// <summary>Required pictograms that are not in <paramref name="chosen"/>.</summary>
    /// <param name="chosen">The pictograms currently chosen for the product.</param>
    /// <returns>The missing pictograms, in official order.</returns>
    public IReadOnlyList<Pictogram> MissingFrom(IEnumerable<Pictogram> chosen)
    {
        ArgumentNullException.ThrowIfNull(chosen);
        var chosenSet = chosen.ToHashSet();
        return Required.Where(pictogram => !chosenSet.Contains(pictogram)).ToList();
    }

    /// <summary>
    /// Chosen pictograms that none of the hazard statements require.
    /// Not reported while some codes have no known rule, because those could be the reason.
    /// </summary>
    /// <param name="chosen">The pictograms currently chosen for the product.</param>
    /// <returns>The unexpected pictograms, in official order.</returns>
    public IReadOnlyList<Pictogram> UnexpectedIn(IEnumerable<Pictogram> chosen)
    {
        ArgumentNullException.ThrowIfNull(chosen);
        if (CodesWithoutRule.Count > 0)
        {
            return [];
        }

        var chosenSet = chosen.ToHashSet();
        var requiredSet = Required.ToHashSet();
        return Enum.GetValues<Pictogram>()
            .Where(pictogram => chosenSet.Contains(pictogram) && !requiredSet.Contains(pictogram))
            .ToList();
    }
}
