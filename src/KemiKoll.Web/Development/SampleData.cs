using KemiKoll.Application.Products;
using KemiKoll.Application.SafetyDataSheets;

namespace KemiKoll.Web.Development;

/// <summary>
/// Adds a few example products and data sheets when the app runs in Development
/// and the inventory is empty, so there is always something to look at.
/// </summary>
/// <remarks>
/// Everything goes through the normal use cases, so the examples follow exactly
/// the same rules as data entered by a user.
/// The hazard codes are typical values for these product types, for demonstration only.
/// The examples are chosen so that every data sheet status appears in the list.
/// </remarks>
internal static class SampleData
{
    /// <summary>An example product and, optionally, the revision date of its data sheet.</summary>
    private sealed record Example(AddProductCommand Product, DateOnly? DataSheetRevisionDate);

    private static readonly Example[] Examples =
    [
        // Up to date: revised recently.
        new(new("Acetone", "Exempel Kemi AB", ["H225", "H319", "H336", "EUH066"], ["GHS02", "GHS07"]),
            new DateOnly(2025, 3, 10)),

        // Due for review: older than the default review age of 3 years.
        new(new("Brake cleaner", "Verkstadsprodukter AB", ["H225", "H304", "H315", "H336", "H411"], ["GHS02", "GHS07", "GHS08", "GHS09"]),
            new DateOnly(2021, 6, 1)),

        // Missing: hazardous, but no data sheet registered.
        new(new("Cutting fluid", "Verkstadsprodukter AB", ["H315", "H317", "H412"], ["GHS07"]),
            null),

        // Not required: not hazardous and no data sheet.
        new(new("Hand cleaner", "Exempel Kemi AB", [], []),
            null),
    ];

    /// <summary>Adds the example products and their data sheets, unless products already exist.</summary>
    /// <param name="services">The application's service provider.</param>
    /// <returns>A task that completes when all examples are added.</returns>
    /// <exception cref="InvalidOperationException">An example is invalid (a bug in this file).</exception>
    public static async Task SeedAsync(IServiceProvider services)
    {
        // Use cases are registered per request (scoped), so a scope is needed outside a request.
        using var scope = services.CreateScope();

        // With a database the data survives restarts, so only add examples to an empty inventory.
        var products = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        if ((await products.ListAsync()).Count > 0)
        {
            return;
        }

        var addProduct = scope.ServiceProvider.GetRequiredService<AddProduct>();
        var addSheet = scope.ServiceProvider.GetRequiredService<AddSafetyDataSheet>();

        foreach (var example in Examples)
        {
            var productResult = await addProduct.ExecuteAsync(example.Product);
            if (productResult.ProductId is not { } productId)
            {
                throw new InvalidOperationException(
                    $"Sample product '{example.Product.Name}' is invalid: {string.Join(" ", productResult.Errors)}");
            }

            if (example.DataSheetRevisionDate is not { } revisionDate)
            {
                continue;
            }

            var fileName = example.Product.Name.ToLowerInvariant().Replace(' ', '-') + "-sds.pdf";
            var sheetResult = await addSheet.ExecuteAsync(
                new AddSafetyDataSheetCommand(productId, revisionDate, "sv", fileName));

            if (!sheetResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Sample data sheet for '{example.Product.Name}' is invalid: {string.Join(" ", sheetResult.Errors)}");
            }
        }
    }
}
