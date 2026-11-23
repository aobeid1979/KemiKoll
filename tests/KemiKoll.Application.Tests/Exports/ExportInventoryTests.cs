using KemiKoll.Application.Exports;
using KemiKoll.Application.Products;
using KemiKoll.Application.Tests.Products;
using KemiKoll.Application.Tests.TestDoubles;
using KemiKoll.Domain.Hazards;
using KemiKoll.Domain.Products;

namespace KemiKoll.Application.Tests.Exports;

public class ExportInventoryTests
{
    private static readonly DateOnly Today = new(2026, 11, 23);

    private readonly FakeProductRepository _products = new();
    private readonly RecordingWriter _writer = new();
    private readonly ExportInventory _export;

    public ExportInventoryTests()
    {
        var time = new FixedTimeProvider(Today);
        _export = new ExportInventory(new ListProducts(_products, new FakeSafetyDataSheetRepository(), time), _writer, time);
    }

    [Fact]
    public async Task ToExcelAsync_NamesFileAfterTodayAndReturnsWrittenBytes()
    {
        var file = await _export.ToExcelAsync();

        Assert.Equal("kemikoll-inventory-2026-11-23.xlsx", file.FileName);
        Assert.Equal(ExportInventory.ExcelContentType, file.ContentType);
        Assert.Equal(RecordingWriter.Bytes, file.Content);
        Assert.Equal(Today, _writer.ExportDate);
    }

    [Fact]
    public async Task ToExcelAsync_PassesAllProductsSortedByName()
    {
        _products.Products.Add(ChemicalProduct.Create("Bensin", "Aspen AB", []));
        _products.Products.Add(ChemicalProduct.Create("aceton", "Kemetyl AB", []));

        await _export.ToExcelAsync();

        string[] expected = ["aceton", "Bensin"];
        Assert.Equal(expected, _writer.Products.Select(p => p.Name));
    }

    [Fact]
    public async Task ToExcelAsync_Selection_ExportsOnlyMatchingProductsAndDescribesFilter()
    {
        _products.Products.Add(ChemicalProduct.Create("Bensin", "Aspen AB", [HazardStatement.Parse("H224")]));
        _products.Products.Add(ChemicalProduct.Create("Alkylat", "Aspen AB", []));
        _products.Products.Add(ChemicalProduct.Create("Aceton", "Kemetyl AB", [HazardStatement.Parse("H225")]));

        await _export.ToExcelAsync(new InventorySelection(" aspen ", ProductFilter.Hazardous));

        Assert.Equal("Bensin", Assert.Single(_writer.Products).Name);
        Assert.Equal("Hazardous · Search: aspen", _writer.Sheet!.Selection);
    }

    [Fact]
    public async Task ToExcelAsync_NoSelection_HasNoSelectionText()
    {
        await _export.ToExcelAsync();

        Assert.Null(_writer.Sheet!.Selection);
    }

    // Remembers what it was asked to write and writes a few fixed bytes.
    private sealed class RecordingWriter : IInventorySpreadsheetWriter
    {
        public static readonly byte[] Bytes = [1, 2, 3];

        public InventorySheet? Sheet { get; private set; }

        public IReadOnlyList<ProductSummary> Products => Sheet?.Products ?? [];

        public DateOnly ExportDate => Sheet?.ExportDate ?? default;

        public void Write(InventorySheet sheet, Stream output)
        {
            Sheet = sheet;
            output.Write(Bytes);
        }
    }
}
