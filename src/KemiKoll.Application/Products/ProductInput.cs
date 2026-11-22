using KemiKoll.Domain.Hazards;

namespace KemiKoll.Application.Products;

/// <summary>
/// Turns the codes a user typed into domain values, collecting a readable error for each invalid code.
/// Shared by adding and editing a product, so both follow exactly the same rules.
/// </summary>
internal static class ProductInput
{
    /// <summary>Parses hazard statement codes such as "H225".</summary>
    /// <param name="codes">The codes as typed. <see langword="null"/> means none.</param>
    /// <param name="errors">Receives one message per invalid code.</param>
    /// <returns>The valid hazard statements, in input order.</returns>
    public static List<HazardStatement> ParseHazardStatements(IReadOnlyList<string>? codes, List<string> errors)
    {
        var statements = new List<HazardStatement>();
        foreach (var code in codes ?? [])
        {
            if (HazardStatement.TryParse(code, out var statement))
            {
                statements.Add(statement);
            }
            else
            {
                errors.Add($"'{code}' is not a valid hazard statement code (for example H225 or EUH066).");
            }
        }

        return statements;
    }

    /// <summary>Parses pictogram codes such as "GHS02".</summary>
    /// <param name="codes">The codes as typed. <see langword="null"/> means none.</param>
    /// <param name="errors">Receives one message per invalid code.</param>
    /// <returns>The valid pictograms, in input order.</returns>
    public static List<Pictogram> ParsePictograms(IReadOnlyList<string>? codes, List<string> errors)
    {
        var pictograms = new List<Pictogram>();
        foreach (var code in codes ?? [])
        {
            if (PictogramCodes.TryParse(code, out var pictogram))
            {
                pictograms.Add(pictogram);
            }
            else
            {
                errors.Add($"'{code}' is not a valid pictogram code (GHS01 to GHS09).");
            }
        }

        return pictograms;
    }
}
