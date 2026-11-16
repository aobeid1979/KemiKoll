using KemiKoll.Application.Products;
using KemiKoll.Domain.Hazards;
using KemiKoll.Domain.Products;

namespace KemiKoll.Application.Tests.Products;

public class ListProductsTests
{
    private readonly FakeProductRepository _repository = new();
    private readonly ListProducts _listProducts;

    public ListProductsTests() => _listProducts = new ListProducts(_repository);

    [Fact]
    public async Task ExecuteAsync_NoProducts_ReturnsEmptyList()
    {
        Assert.Empty(await _listProducts.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsProductsSortedByNameIgnoringCase()
    {
        _repository.Products.Add(ChemicalProduct.Create("cutting fluid", "Supplier"));
        _repository.Products.Add(ChemicalProduct.Create("Acetone", "Supplier"));
        _repository.Products.Add(ChemicalProduct.Create("Brake cleaner", "Supplier"));

        var result = await _listProducts.ExecuteAsync();

        string[] expected = ["Acetone", "Brake cleaner", "cutting fluid"];
        Assert.Equal(expected, result.Select(p => p.Name));
    }

    [Fact]
    public async Task ExecuteAsync_MapsAllFields()
    {
        var product = ChemicalProduct.Create("Acetone", "ACME Kemi AB", [HazardStatement.Parse("H225")]);
        product.AddPictogram(Pictogram.Flame);
        _repository.Products.Add(product);

        var summary = Assert.Single(await _listProducts.ExecuteAsync());

        Assert.Equal(product.Id, summary.Id);
        Assert.Equal("Acetone", summary.Name);
        Assert.Equal("ACME Kemi AB", summary.Supplier);
        Assert.Equal("H225", Assert.Single(summary.HazardCodes));
        Assert.Equal("GHS02", Assert.Single(summary.PictogramCodes));
        Assert.True(summary.IsHazardous);
    }
}
