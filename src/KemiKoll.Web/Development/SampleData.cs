using KemiKoll.Application.Products;

namespace KemiKoll.Web.Development;

/// <summary>
/// Adds a few example products when the app runs in Development,
/// so the product list is not empty while the in-memory storage is used.
/// </summary>
/// <remarks>
/// The products go through the normal <see cref="AddProduct"/> use case,
/// so they follow exactly the same rules as products entered by a user.
/// The hazard codes are typical values for these product types, for demonstration only.
/// </remarks>
internal static class SampleData
{
    private static readonly AddProductCommand[] Products =
    [
        new("Acetone", "Exempel Kemi AB", ["H225", "H319", "H336", "EUH066"], ["GHS02", "GHS07"]),
        new("Brake cleaner", "Verkstadsprodukter AB", ["H225", "H304", "H315", "H336", "H411"], ["GHS02", "GHS07", "GHS08", "GHS09"]),
        new("Hand cleaner", "Exempel Kemi AB", [], []),
    ];

    /// <summary>Adds the example products.</summary>
    /// <param name="services">The application's service provider.</param>
    /// <returns>A task that completes when all products are added.</returns>
    /// <exception cref="InvalidOperationException">An example product is invalid (a bug in this file).</exception>
    public static async Task SeedAsync(IServiceProvider services)
    {
        // Use cases are registered per request (scoped), so a scope is needed outside a request.
        using var scope = services.CreateScope();
        var addProduct = scope.ServiceProvider.GetRequiredService<AddProduct>();

        foreach (var command in Products)
        {
            var result = await addProduct.ExecuteAsync(command);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Sample product '{command.Name}' is invalid: {string.Join(" ", result.Errors)}");
            }
        }
    }
}
