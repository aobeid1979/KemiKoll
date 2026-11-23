using KemiKoll.Application.Products;

namespace KemiKoll.Application.Exports;

/// <summary>
/// Which products to export: the same search text and quick filter as on the product list,
/// so the user exports what they see.
/// </summary>
/// <param name="SearchText">The search text, or <see langword="null"/>/empty for no search.</param>
/// <param name="Filter">The quick filter.</param>
public sealed record InventorySelection(string? SearchText = null, ProductFilter Filter = ProductFilter.All)
{
    /// <summary>Every product.</summary>
    public static InventorySelection Everything { get; } = new();

    /// <summary><see langword="true"/> when nothing is filtered out.</summary>
    public bool IsEverything => string.IsNullOrWhiteSpace(SearchText) && Filter == ProductFilter.All;

    /// <summary>
    /// A short text for the file, for example "Needs attention · Search: aspen",
    /// or <see langword="null"/> when every product is exported.
    /// </summary>
    /// <returns>The description, or <see langword="null"/>.</returns>
    public string? Describe()
    {
        var parts = new List<string>();
        if (Filter == ProductFilter.NeedsAttention)
        {
            parts.Add("Needs attention");
        }
        else if (Filter == ProductFilter.Hazardous)
        {
            parts.Add("Hazardous");
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            parts.Add($"Search: {SearchText.Trim()}");
        }

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }
}
