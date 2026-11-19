using KemiKoll.Application.SafetyDataSheets;
using KemiKoll.Application.Tests.Products;
using KemiKoll.Application.Tests.TestDoubles;
using KemiKoll.Domain.Products;

namespace KemiKoll.Application.Tests.SafetyDataSheets;

public class AddSafetyDataSheetTests
{
    private readonly FakeProductRepository _products = new();
    private readonly FakeSafetyDataSheetRepository _sheets = new();
    private readonly AddSafetyDataSheet _addSheet;
    private readonly ChemicalProduct _product = ChemicalProduct.Create("Acetone", "Supplier");

    public AddSafetyDataSheetTests()
    {
        _products.Products.Add(_product);
        _addSheet = new AddSafetyDataSheet(_products, _sheets);
    }

    [Fact]
    public async Task ExecuteAsync_ValidInput_SavesSheet()
    {
        var result = await _addSheet.ExecuteAsync(
            new AddSafetyDataSheetCommand(_product.Id, new DateOnly(2025, 3, 10), "SV", "acetone.pdf"));

        Assert.True(result.Succeeded);
        var saved = Assert.Single(_sheets.Sheets);
        Assert.Equal(result.SheetId, saved.Id);
        Assert.Equal(_product.Id, saved.ProductId);
        Assert.Equal("sv", saved.LanguageCode);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownProduct_ReturnsErrorAndSavesNothing()
    {
        var result = await _addSheet.ExecuteAsync(
            new AddSafetyDataSheetCommand(Guid.NewGuid(), new DateOnly(2025, 3, 10), "sv", "a.pdf"));

        Assert.False(result.Succeeded);
        Assert.Equal("The product does not exist.", Assert.Single(result.Errors));
        Assert.Empty(_sheets.Sheets);
    }

    [Fact]
    public async Task ExecuteAsync_NotAPdf_ReturnsReadableError()
    {
        var result = await _addSheet.ExecuteAsync(
            new AddSafetyDataSheetCommand(_product.Id, new DateOnly(2025, 3, 10), "sv", "acetone.docx"));

        Assert.False(result.Succeeded);
        var error = Assert.Single(result.Errors);
        Assert.StartsWith("FileName:", error, StringComparison.Ordinal);
        Assert.DoesNotContain("Parameter", error, StringComparison.Ordinal);
        Assert.Empty(_sheets.Sheets);
    }

    [Fact]
    public async Task ExecuteAsync_NullCommand_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _addSheet.ExecuteAsync(null!));
    }
}
