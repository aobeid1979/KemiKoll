using KemiKoll.Application.Products;
using KemiKoll.Domain.Hazards;

namespace KemiKoll.Application.Tests.Products;

public class AddProductTests
{
    private readonly FakeProductRepository _repository = new();
    private readonly AddProduct _addProduct;

    public AddProductTests() => _addProduct = new AddProduct(_repository);

    private static AddProductCommand Command(
        string name = "Degreaser X",
        string supplier = "ACME Kemi AB",
        string[]? hazardCodes = null,
        string[]? pictogramCodes = null) =>
        new(name, supplier, hazardCodes ?? [], pictogramCodes ?? []);

    [Fact]
    public async Task ExecuteAsync_ValidInput_SavesProductAndReturnsId()
    {
        var result = await _addProduct.ExecuteAsync(
            Command(hazardCodes: ["h225", "H315"], pictogramCodes: ["ghs02", "GHS07"]));

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        var saved = Assert.Single(_repository.Products);
        Assert.Equal(result.ProductId, saved.Id);
        Assert.Equal("Degreaser X", saved.Name);
        string[] expectedCodes = ["H225", "H315"];
        Pictogram[] expectedPictograms = [Pictogram.Flame, Pictogram.ExclamationMark];
        Assert.Equal(expectedCodes, saved.HazardStatements.Select(s => s.Code));
        Assert.Equal(expectedPictograms, saved.Pictograms);
    }

    [Fact]
    public async Task ExecuteAsync_NoCodes_SavesNonHazardousProduct()
    {
        var result = await _addProduct.ExecuteAsync(Command());

        Assert.True(result.Succeeded);
        Assert.False(Assert.Single(_repository.Products).IsHazardous);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidHazardCode_ReturnsErrorAndSavesNothing()
    {
        var result = await _addProduct.ExecuteAsync(Command(hazardCodes: ["H225", "P210"]));

        Assert.False(result.Succeeded);
        Assert.Null(result.ProductId);
        Assert.Contains(result.Errors, e => e.Contains("'P210'", StringComparison.Ordinal));
        Assert.Empty(_repository.Products);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidPictogramCode_ReturnsError()
    {
        var result = await _addProduct.ExecuteAsync(Command(pictogramCodes: ["GHS10"]));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("'GHS10'", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_EmptyName_ReturnsReadableError()
    {
        var result = await _addProduct.ExecuteAsync(Command(name: " "));

        Assert.False(result.Succeeded);
        var error = Assert.Single(result.Errors);
        Assert.StartsWith("Name:", error, StringComparison.Ordinal);
        Assert.DoesNotContain("Parameter", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_SeveralProblems_ReturnsAllErrors()
    {
        var result = await _addProduct.ExecuteAsync(
            Command(name: "", hazardCodes: ["X1"], pictogramCodes: ["GHS00"]));

        Assert.Equal(3, result.Errors.Count);
        Assert.Empty(_repository.Products);
    }

    [Fact]
    public async Task ExecuteAsync_NullCommand_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _addProduct.ExecuteAsync(null!));
    }
}
