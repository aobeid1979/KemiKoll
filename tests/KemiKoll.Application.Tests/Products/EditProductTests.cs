using KemiKoll.Application.Products;
using KemiKoll.Application.Tests.TestDoubles;
using KemiKoll.Domain.Hazards;
using KemiKoll.Domain.Products;
using KemiKoll.Domain.SafetyDataSheets;

namespace KemiKoll.Application.Tests.Products;

public class EditProductTests
{
    private static readonly DateTimeOffset Added = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 23);
    private static readonly DateTimeOffset Now = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero); // FixedTimeProvider: noon UTC

    private readonly FakeProductRepository _products = new();
    private readonly FakeSafetyDataSheetRepository _sheets = new();
    private readonly EditProduct _editProduct;
    private readonly ChemicalProduct _ethanol;

    public EditProductTests()
    {
        _editProduct = new EditProduct(_products, _sheets, new FixedTimeProvider(Today));

        // The mistake found in real use: H225 with GHS03 instead of GHS02.
        _ethanol = ChemicalProduct.Create("Ethanol", "Amfo Company", [HazardStatement.Parse("H225")], Added);
        _ethanol.AddPictogram(Pictogram.FlameOverCircle);
        _products.Products.Add(_ethanol);
    }

    private UpdateProductCommand Command(
        string name = "Ethanol",
        string supplier = "Amfo Company",
        string[]? hazardCodes = null,
        string[]? pictogramCodes = null) =>
        new(_ethanol.Id, name, supplier, hazardCodes ?? ["H225"], pictogramCodes ?? ["GHS02"]);

    [Fact]
    public async Task GetAsync_ExistingProduct_ReturnsDetailsAsText()
    {
        var details = await _editProduct.GetAsync(_ethanol.Id);

        Assert.NotNull(details);
        Assert.Equal("Ethanol", details.Name);
        Assert.Equal("Amfo Company", details.Supplier);
        Assert.Equal("H225", Assert.Single(details.HazardCodes));
        Assert.Equal("GHS03", Assert.Single(details.PictogramCodes));
    }

    [Fact]
    public async Task GetAsync_UnknownProduct_ReturnsNull()
    {
        Assert.Null(await _editProduct.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_ChangesAndSavesProduct()
    {
        var result = await _editProduct.UpdateAsync(Command(name: "Ethanol absolute", pictogramCodes: ["ghs02"]));

        Assert.True(result.Succeeded);
        Assert.Equal(1, _products.UpdateCount);
        Assert.Equal("Ethanol absolute", _ethanol.Name);
        Assert.Equal(Pictogram.Flame, Assert.Single(_ethanol.Pictograms));
    }

    [Fact]
    public async Task UpdateAsync_InvalidCode_ChangesNothing()
    {
        var result = await _editProduct.UpdateAsync(Command(name: "Changed", hazardCodes: ["H225", "P210"]));

        Assert.False(result.Succeeded);
        Assert.False(result.NotFound);
        Assert.Contains(result.Errors, e => e.Contains("'P210'", StringComparison.Ordinal));
        Assert.Equal("Ethanol", _ethanol.Name);
        Assert.Equal(0, _products.UpdateCount);
    }

    [Fact]
    public async Task UpdateAsync_EmptyName_ReturnsReadableErrorAndChangesNothing()
    {
        var result = await _editProduct.UpdateAsync(Command(name: " "));

        Assert.StartsWith("Name:", Assert.Single(result.Errors), StringComparison.Ordinal);
        Assert.Equal("Ethanol", _ethanol.Name);
        Assert.Equal(0, _products.UpdateCount);
    }

    [Fact]
    public async Task UpdateAsync_UnknownProduct_ReturnsNotFound()
    {
        var result = await _editProduct.UpdateAsync(Command() with { Id = Guid.NewGuid() });

        Assert.False(result.Succeeded);
        Assert.True(result.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_RemovesProductAndItsDataSheets()
    {
        _sheets.Sheets.Add(SafetyDataSheet.Create(_ethanol.Id, new DateOnly(2025, 8, 7), "en", "ethanol.pdf"));
        var other = ChemicalProduct.Create("Acetone", "Supplier");
        _products.Products.Add(other);
        _sheets.Sheets.Add(SafetyDataSheet.Create(other.Id, new DateOnly(2025, 3, 10), "sv", "acetone.pdf"));

        var deleted = await _editProduct.DeleteAsync(_ethanol.Id);

        Assert.True(deleted);
        Assert.Equal(other, Assert.Single(_products.Products));
        Assert.Equal(other.Id, Assert.Single(_sheets.Sheets).ProductId);
    }

    [Fact]
    public async Task DeleteAsync_UnknownProduct_ReturnsFalse()
    {
        Assert.False(await _editProduct.DeleteAsync(Guid.NewGuid()));
        Assert.Single(_products.Products);
    }

    [Fact]
    public async Task UpdateAsync_RealChange_SetsLastChangedAtToNow()
    {
        await _editProduct.UpdateAsync(Command(name: "Ethanol absolute"));

        var details = await _editProduct.GetAsync(_ethanol.Id);
        Assert.Equal(Added, details!.AddedAt);
        Assert.Equal(Now, details.LastChangedAt);
    }

    [Fact]
    public async Task UpdateAsync_NoChange_KeepsLastChangedAt()
    {
        // Make the product equal to the command (GHS02 instead of GHS03), dated when it was added.
        _ethanol.Update("Ethanol", "Amfo Company", [HazardStatement.Parse("H225")], [Pictogram.Flame], Added);

        var result = await _editProduct.UpdateAsync(Command());

        Assert.True(result.Succeeded);
        Assert.Equal(Added, _ethanol.LastChangedAt);
    }
}
