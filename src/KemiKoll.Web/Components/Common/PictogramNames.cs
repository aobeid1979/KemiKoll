using System.Text.RegularExpressions;

using KemiKoll.Domain.Hazards;

namespace KemiKoll.Web.Components.Common;

/// <summary>
/// Readable names for GHS pictograms, used as labels and tooltips in the UI.
/// </summary>
internal static partial class PictogramNames
{
    /// <summary>
    /// The CLP name with spaces, for example "Skull and crossbones" for <see cref="Pictogram.SkullAndCrossbones"/>.
    /// </summary>
    /// <param name="pictogram">The pictogram.</param>
    /// <returns>The name in sentence case.</returns>
    public static string DisplayName(Pictogram pictogram)
    {
        // "SkullAndCrossbones" → "Skull And Crossbones" → "Skull and crossbones"
        var words = CapitalLetter().Replace(pictogram.ToString(), " $1");
        return words[0] + words[1..].ToLowerInvariant();
    }

    /// <summary>
    /// The code and name, for example "GHS06 – Skull and crossbones".
    /// Returns the code unchanged if it is not a valid pictogram code.
    /// </summary>
    /// <param name="code">A pictogram code such as "GHS06".</param>
    /// <returns>A label for tooltips and alternative text.</returns>
    public static string Label(string code) =>
        PictogramCodes.TryParse(code, out var pictogram)
            ? $"{pictogram.ToCode()} – {DisplayName(pictogram)}"
            : code;

    // Every capital letter except the first one.
    [GeneratedRegex("(?<!^)([A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex CapitalLetter();
}
