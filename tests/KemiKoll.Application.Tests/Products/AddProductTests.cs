using KemiKoll.Application.Products;
using KemiKoll.Application.Tests.TestDoubles;
using KemiKoll.Domain.Hazards;

namespace KemiKoll.Application.Tests.Products;

public class AddProductTests
{
    private readonly FakeProductRepository _repository = new();
    private readonly FakeSafetyDataSheetRepository _sheets = new();
    private readonly AddProduct _addProduct;

    public AddProductTests() => _addProduct = new AddProduct(_repository, _sheets, new FixedTimeProvider(new DateOnly(2026, 11, 23)));

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

    [Fact]
    public async Task ExecuteAsync_WithDataSheet_SavesProductAndDataSheet()
    {
        var command = Command() with { DataSheet = new NewDataSheet(new DateOnly(2025, 3, 10), "sv", "acetone-sds.pdf") };

        var result = await _addProduct.ExecuteAsync(command);

        Assert.True(result.Succeeded);
        var sheet = Assert.Single(_sheets.Sheets);
        Assert.Equal(result.ProductId, sheet.ProductId);
        Assert.Equal(new DateOnly(2025, 3, 10), sheet.RevisionDate);
        Assert.Equal("acetone-sds.pdf", sheet.FileName);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutDataSheet_SavesNoDataSheet()
    {
        await _addProduct.ExecuteAsync(Command());

        Assert.Empty(_sheets.Sheets);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidDataSheet_SavesNothing()
    {
        var command = Command() with { DataSheet = new NewDataSheet(new DateOnly(2025, 3, 10), "swedish", "sheet.pdf") };

        var result = await _addProduct.ExecuteAsync(command);

        Assert.False(result.Succeeded);
        Assert.StartsWith("Data sheet – LanguageCode:", Assert.Single(result.Errors), StringComparison.Ordinal);
        Assert.Empty(_repository.Products);
        Assert.Empty(_sheets.Sheets);
    }

    [Fact]
    public async Task ExecuteAsync_SameNameAndSupplier_ReturnsDuplicateAndSavesNothing()
    {
        var first = await _addProduct.ExecuteAsync(Command());

        var second = await _addProduct.ExecuteAsync(
            Command(name: "  degreaser x ", supplier: "ACME KEMI AB") with
            { DataSheet = new NewDataSheet(new DateOnly(2025, 1, 1), "sv", "x.pdf") });

        Assert.False(second.Succeeded);
        Assert.True(second.IsDuplicate);
        Assert.Equal(first.ProductId, second.DuplicateOfProductId);
        Assert.Empty(second.Errors);
        Assert.Single(_repository.Products);
        Assert.Empty(_sheets.Sheets);
    }

    [Fact]
    public async Task ExecuteAsync_SameNameAndSupplier_AllowDuplicate_SavesSecondProduct()
    {
        await _addProduct.ExecuteAsync(Command());

        var second = await _addProduct.ExecuteAsync(Command() with { AllowDuplicate = true });

        Assert.True(second.Succeeded);
        Assert.Equal(2, _repository.Products.Count);
    }

    [Fact]
    public async Task ExecuteAsync_SameNameOtherSupplier_IsNotDuplicate()
    {
        await _addProduct.ExecuteAsync(Command());

        var second = await _addProduct.ExecuteAsync(Command(supplier: "Other Kemi AB"));

        Assert.True(second.Succeeded);
        Assert.False(second.IsDuplicate);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidInput_ReportsErrorsBeforeDuplicateCheck()
    {
        await _addProduct.ExecuteAsync(Command());

        var second = await _addProduct.ExecuteAsync(Command(hazardCodes: ["P210"]));

        Assert.False(second.IsDuplicate);
        Assert.NotEmpty(second.Errors);
    }

    [Fact]
    public async Task ExecuteAsync_SetsAddedAtFromTheClock()
    {
        await _addProduct.ExecuteAsync(Command());

        var expected = new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
        var saved = Assert.Single(_repository.Products);
        Assert.Equal(expected, saved.AddedAt);
        Assert.Equal(expected, saved.LastChangedAt);
    }
}
