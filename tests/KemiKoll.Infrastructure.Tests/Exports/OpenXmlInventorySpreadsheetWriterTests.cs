using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using KemiKoll.Application.Products;
using KemiKoll.Infrastructure.Exports;

namespace KemiKoll.Infrastructure.Tests.Exports;

public class OpenXmlInventorySpreadsheetWriterTests
{
    private static readonly DateOnly ExportDate = new(2026, 11, 23);

    private static readonly ProductSummary Bensin = new(
        Guid.NewGuid(), "Aspen 4T Bensin", "Lantmännen Aspen AB", ["H224", "H304"], ["GHS02", "GHS08"],
        true, DataSheetStatus.UpToDate, new DateOnly(2025, 3, 10), [], []);

    private static readonly ProductSummary Oil = new(
        Guid.NewGuid(), "Skärolja", "Ståhl AB", [], [], false, DataSheetStatus.NotRequired, null, [], []);

    // Writes the file and opens it again, as Excel would. Returns the cells by reference ("A1" → text).
    private static Dictionary<string, Cell> WriteAndRead(params ProductSummary[] products)
    {
        using var stream = new MemoryStream();
        new OpenXmlInventorySpreadsheetWriter().Write(products, ExportDate, stream);

        stream.Position = 0;
        using var document = SpreadsheetDocument.Open(stream, isEditable: false);
        var workbookPart = document.WorkbookPart!;
        Assert.Equal("Inventory", workbookPart.Workbook!.Sheets!.Elements<Sheet>().Single().Name!.Value);

        return workbookPart.WorksheetParts.Single().Worksheet!.Descendants<Cell>()
            .ToDictionary(cell => cell.CellReference!.Value!);
    }

    private static string Text(Dictionary<string, Cell> cells, string reference) =>
        cells[reference].InlineString?.InnerText ?? cells[reference].CellValue!.Text;

    [Fact]
    public void Write_TitleExportDateAndHeaders()
    {
        var cells = WriteAndRead(Bensin, Oil);

        Assert.Equal("Chemical inventory (kemikalieförteckning)", Text(cells, "A1"));
        Assert.Equal("Exported 2026-11-23 · 2 products", Text(cells, "A2"));
        Assert.Equal("Product", Text(cells, "A4"));
        Assert.Equal("Revision date", Text(cells, "G4"));
    }

    [Fact]
    public void Write_OneRowPerProduct()
    {
        var cells = WriteAndRead(Bensin, Oil);

        Assert.Equal("Aspen 4T Bensin", Text(cells, "A5"));
        Assert.Equal("Lantmännen Aspen AB", Text(cells, "B5"));
        Assert.Equal("H224, H304", Text(cells, "C5"));
        Assert.Equal("GHS02, GHS08", Text(cells, "D5"));
        Assert.Equal("Yes", Text(cells, "E5"));
        Assert.Equal("Up to date", Text(cells, "F5"));

        Assert.Equal("Skärolja", Text(cells, "A6"));
        Assert.Equal("No", Text(cells, "E6"));
        Assert.Equal("Not required", Text(cells, "F6"));
        Assert.False(cells.ContainsKey("G6"));
    }

    [Fact]
    public void Write_RevisionDate_IsARealExcelDate()
    {
        var cells = WriteAndRead(Bensin);

        var serial = double.Parse(cells["G5"].CellValue!.Text, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(new DateTime(2025, 3, 10), DateTime.FromOADate(serial));
        Assert.NotNull(cells["G5"].StyleIndex);
    }

    [Fact]
    public void Write_NoProducts_StillValidFile()
    {
        var cells = WriteAndRead();

        Assert.Equal("Exported 2026-11-23 · 0 products", Text(cells, "A2"));
        Assert.False(cells.ContainsKey("A5"));
    }
}
