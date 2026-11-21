using KemiKoll.Application.Products;
using KemiKoll.Application.Tests.TestDoubles;
using KemiKoll.Domain.Hazards;
using KemiKoll.Domain.Products;
using KemiKoll.Domain.SafetyDataSheets;

namespace KemiKoll.Application.Tests.Products;

public class ListProductsTests
{
    // Default review policy is 3 years, so with this "today" a data sheet revised
    // on or before 2023-06-01 is due for review.
    private static readonly DateOnly Today = new(2026, 6, 1);

    private readonly FakeProductRepository _products = new();
    private readonly FakeSafetyDataSheetRepository _sheets = new();
    private readonly ListProducts _listProducts;

    public ListProductsTests() =>
        _listProducts = new ListProducts(_products, _sheets, new FixedTimeProvider(Today));

    private ChemicalProduct AddHazardousProduct(string name = "Acetone")
    {
        var product = ChemicalProduct.Create(name, "Supplier", [HazardStatement.Parse("H225")]);
        _products.Products.Add(product);
        return product;
    }

    private void AddSheet(ChemicalProduct product, DateOnly revisionDate) =>
        _sheets.Sheets.Add(SafetyDataSheet.Create(product.Id, revisionDate, "sv", "sheet.pdf"));

    [Fact]
    public async Task ExecuteAsync_NoProducts_ReturnsEmptyList()
    {
        Assert.Empty(await _listProducts.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsProductsSortedByNameIgnoringCase()
    {
        _products.Products.Add(ChemicalProduct.Create("cutting fluid", "Supplier"));
        _products.Products.Add(ChemicalProduct.Create("Acetone", "Supplier"));
        _products.Products.Add(ChemicalProduct.Create("Brake cleaner", "Supplier"));

        var result = await _listProducts.ExecuteAsync();

        string[] expected = ["Acetone", "Brake cleaner", "cutting fluid"];
        Assert.Equal(expected, result.Select(p => p.Name));
    }

    [Fact]
    public async Task ExecuteAsync_MapsAllFields()
    {
        var product = ChemicalProduct.Create("Acetone", "ACME Kemi AB", [HazardStatement.Parse("H225")]);
        product.AddPictogram(Pictogram.Flame);
        _products.Products.Add(product);
        AddSheet(product, new DateOnly(2025, 3, 10));

        var summary = Assert.Single(await _listProducts.ExecuteAsync());

        Assert.Equal(product.Id, summary.Id);
        Assert.Equal("Acetone", summary.Name);
        Assert.Equal("ACME Kemi AB", summary.Supplier);
        Assert.Equal("H225", Assert.Single(summary.HazardCodes));
        Assert.Equal("GHS02", Assert.Single(summary.PictogramCodes));
        Assert.True(summary.IsHazardous);
        Assert.Equal(new DateOnly(2025, 3, 10), summary.DataSheetRevisionDate);
    }

    [Fact]
    public async Task ExecuteAsync_NonHazardousWithoutSheet_IsNotRequired()
    {
        _products.Products.Add(ChemicalProduct.Create("Hand cleaner", "Supplier"));

        var summary = Assert.Single(await _listProducts.ExecuteAsync());

        Assert.Equal(DataSheetStatus.NotRequired, summary.DataSheetStatus);
        Assert.Null(summary.DataSheetRevisionDate);
    }

    [Fact]
    public async Task ExecuteAsync_HazardousWithoutSheet_IsMissing()
    {
        AddHazardousProduct();

        var summary = Assert.Single(await _listProducts.ExecuteAsync());

        Assert.Equal(DataSheetStatus.Missing, summary.DataSheetStatus);
    }

    [Theory]
    [InlineData(2023, 6, 2, DataSheetStatus.UpToDate)]     // due tomorrow
    [InlineData(2023, 6, 1, DataSheetStatus.DueForReview)] // due today
    [InlineData(2020, 1, 1, DataSheetStatus.DueForReview)] // long overdue
    [InlineData(2026, 1, 15, DataSheetStatus.UpToDate)]    // recent
    public async Task ExecuteAsync_WithSheet_UsesDefaultReviewAge(int year, int month, int day, DataSheetStatus expected)
    {
        AddSheet(AddHazardousProduct(), new DateOnly(year, month, day));

        var summary = Assert.Single(await _listProducts.ExecuteAsync());

        Assert.Equal(expected, summary.DataSheetStatus);
    }

    [Fact]
    public async Task ExecuteAsync_SeveralRevisions_UsesNewest()
    {
        var product = AddHazardousProduct();
        AddSheet(product, new DateOnly(2019, 1, 1));
        AddSheet(product, new DateOnly(2025, 9, 1));
        AddSheet(product, new DateOnly(2021, 1, 1));

        var summary = Assert.Single(await _listProducts.ExecuteAsync());

        Assert.Equal(DataSheetStatus.UpToDate, summary.DataSheetStatus);
        Assert.Equal(new DateOnly(2025, 9, 1), summary.DataSheetRevisionDate);
    }

    [Fact]
    public async Task ExecuteAsync_SheetOfAnotherProduct_IsNotUsed()
    {
        var acetone = AddHazardousProduct("Acetone");
        var brakeCleaner = AddHazardousProduct("Brake cleaner");
        AddSheet(acetone, new DateOnly(2025, 1, 1));

        var result = await _listProducts.ExecuteAsync();

        Assert.Equal(DataSheetStatus.UpToDate, result.Single(p => p.Id == acetone.Id).DataSheetStatus);
        Assert.Equal(DataSheetStatus.Missing, result.Single(p => p.Id == brakeCleaner.Id).DataSheetStatus);
    }

    [Fact]
    public async Task ExecuteAsync_WrongPictogram_IsReported()
    {
        // H225 requires GHS02 (flame); GHS03 (flame over circle) is for oxidisers.
        var product = AddHazardousProduct("Ethanol");
        product.AddPictogram(Pictogram.FlameOverCircle);

        var summary = Assert.Single(await _listProducts.ExecuteAsync());

        Assert.Equal("GHS02", Assert.Single(summary.MissingPictogramCodes));
        Assert.Equal("GHS03", Assert.Single(summary.UnexpectedPictogramCodes));
    }

    [Fact]
    public async Task ExecuteAsync_MatchingPictograms_ReportNothing()
    {
        var product = AddHazardousProduct("Acetone");
        product.AddPictogram(Pictogram.Flame);

        var summary = Assert.Single(await _listProducts.ExecuteAsync());

        Assert.Empty(summary.MissingPictogramCodes);
        Assert.Empty(summary.UnexpectedPictogramCodes);
    }
}
