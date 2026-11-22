using KemiKoll.Domain.Hazards;

namespace KemiKoll.Web.Components.Shared;

/// <summary>
/// What the user has entered in the product form. Shared by the "Add product" and
/// "Edit product" pages through <see cref="ProductFormFields"/>.
/// </summary>
/// <remarks>
/// Holds text exactly as typed; validation happens in the use cases when saving.
/// The pictogram check here is only a hint and never blocks saving.
/// </remarks>
public sealed class ProductFormModel
{
    // Characters allowed between hazard codes: comma, semicolon and any whitespace (including new lines).
    private static readonly char[] CodeSeparators = [',', ';', ' ', '\t', '\r', '\n'];

    /// <summary>The trade name as typed.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The supplier as typed.</summary>
    public string Supplier { get; set; } = string.Empty;

    /// <summary>The hazard codes as typed, for example "H225, H319".</summary>
    public string HazardCodes { get; set; } = string.Empty;

    /// <summary>The ticked pictograms.</summary>
    public HashSet<Pictogram> SelectedPictograms { get; } = [];

    /// <summary>The typed hazard codes as a list (still unvalidated).</summary>
    public IReadOnlyList<string> HazardCodeList() =>
        HazardCodes.Split(CodeSeparators, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>The ticked pictograms as codes, in official order (GHS01 … GHS09).</summary>
    public IReadOnlyList<string> PictogramCodeList() =>
        Enum.GetValues<Pictogram>().Where(SelectedPictograms.Contains).Select(p => p.ToCode()).ToList();

    /// <summary>Replaces the ticked pictograms with the given codes; invalid codes are ignored.</summary>
    /// <param name="codes">Pictogram codes such as "GHS02".</param>
    public void SetPictograms(IEnumerable<string> codes)
    {
        SelectedPictograms.Clear();
        foreach (var pictogramCode in codes)
        {
            if (PictogramCodes.TryParse(pictogramCode, out var pictogram))
            {
                SelectedPictograms.Add(pictogram);
            }
        }
    }

    /// <summary>The CLP pictograms required by the valid hazard codes typed so far.</summary>
    public PictogramRequirement RequiredPictograms() =>
        PictogramRules.For(HazardCodeList()
            .Select(code => HazardStatement.TryParse(code, out var statement) ? statement : null)
            .OfType<HazardStatement>());
}
