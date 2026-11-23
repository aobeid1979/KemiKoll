using System.Globalization;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using KemiKoll.Application.Exports;
using KemiKoll.Application.Products;

namespace KemiKoll.Infrastructure.Exports;

/// <summary>
/// Writes the chemical inventory as an .xlsx file with the Open XML SDK (Microsoft, MIT license).
/// </summary>
/// <remarks>
/// Layout of the single sheet "Inventory":
/// <code>
/// Row 1  Chemical inventory (kemikalieförteckning)          (title)
/// Row 2  Exported 2026-11-23 · 11 products
/// Row 4  Product | Supplier | Hazard statements | …          (bold, filter buttons)
/// Row 5… one row per product                                 (header stays visible when scrolling)
/// </code>
/// Text is written as inline strings, so the file needs no shared-string table.
/// </remarks>
public sealed class OpenXmlInventorySpreadsheetWriter : IInventorySpreadsheetWriter
{
    private const int HeaderRow = 4;

    // Style indexes into the CellFormats created in CreateStylesheet.
    private const uint NormalStyle = 0;
    private const uint HeaderStyle = 1;
    private const uint DateStyle = 2;
    private const uint TitleStyle = 3;

    // Column header and width (in characters) for columns A, B, C …
    private static readonly (string Header, double Width)[] ColumnLayout =
    [
        ("Product", 32),
        ("Supplier", 28),
        ("Hazard statements", 24),
        ("Pictograms", 22),
        ("Hazardous", 11),
        ("Data sheet", 16),
        ("Revision date", 14),
    ];

    /// <inheritdoc/>
    public void Write(IReadOnlyList<ProductSummary> products, DateOnly exportDate, Stream output)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(output);

        using var document = SpreadsheetDocument.Create(output, SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet = CreateStylesheet();

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var sheetData = new SheetData();

        var exported = exportDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        sheetData.Append(
            NewRow(1, TextCell("A1", "Chemical inventory (kemikalieförteckning)", TitleStyle)),
            NewRow(2, TextCell("A2", $"Exported {exported} · {products.Count} products", NormalStyle)),
            NewRow(HeaderRow, ColumnLayout.Select((column, i) => TextCell(Reference(i, HeaderRow), column.Header, HeaderStyle))));

        var rowNumber = HeaderRow;
        foreach (var product in products)
        {
            rowNumber++;
            sheetData.Append(NewRow(rowNumber, ProductCells(product, rowNumber)));
        }

        // Element order inside <worksheet> is fixed by the file format: views, columns, data, filter.
        worksheetPart.Worksheet = new Worksheet(
            new SheetViews(FrozenHeaderView()),
            new Columns(ColumnLayout.Select((column, i) => new Column
            {
                Min = (uint)(i + 1),
                Max = (uint)(i + 1),
                Width = column.Width,
                CustomWidth = true,
            })),
            sheetData,
            new AutoFilter { Reference = $"A{HeaderRow}:{Reference(ColumnLayout.Length - 1, rowNumber)}" });

        workbookPart.Workbook.AppendChild(new Sheets(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Inventory",
        }));
    }

    private static IEnumerable<Cell> ProductCells(ProductSummary product, int row)
    {
        yield return TextCell(Reference(0, row), product.Name, NormalStyle);
        yield return TextCell(Reference(1, row), product.Supplier, NormalStyle);
        yield return TextCell(Reference(2, row), string.Join(", ", product.HazardCodes), NormalStyle);
        yield return TextCell(Reference(3, row), string.Join(", ", product.PictogramCodes), NormalStyle);
        yield return TextCell(Reference(4, row), product.IsHazardous ? "Yes" : "No", NormalStyle);
        yield return TextCell(Reference(5, row), StatusText(product.DataSheetStatus), NormalStyle);
        if (product.DataSheetRevisionDate is { } revision)
        {
            yield return DateCell(Reference(6, row), revision);
        }
    }

    // The same words as on the product list page.
    private static string StatusText(DataSheetStatus status) => status switch
    {
        DataSheetStatus.UpToDate => "Up to date",
        DataSheetStatus.DueForReview => "Due for review",
        DataSheetStatus.Missing => "Missing",
        _ => "Not required",
    };

    private static Row NewRow(int number, params IEnumerable<Cell> cells) =>
        new(cells) { RowIndex = (uint)number };

    private static Cell TextCell(string reference, string text, uint style) => new()
    {
        CellReference = reference,
        DataType = CellValues.InlineString,
        InlineString = new InlineString(new Text(text) { Space = SpaceProcessingModeValues.Preserve }),
        StyleIndex = style,
    };

    // Excel stores dates as numbers (days since 1900); the date style shows them as dates.
    private static Cell DateCell(string reference, DateOnly date) => new()
    {
        CellReference = reference,
        CellValue = new CellValue(date.ToDateTime(TimeOnly.MinValue).ToOADate()),
        StyleIndex = DateStyle,
    };

    // "A4", "G15" …: column index 0 = A. The inventory has fewer than 26 columns.
    private static string Reference(int columnIndex, int row) =>
        $"{(char)('A' + columnIndex)}{row.ToString(CultureInfo.InvariantCulture)}";

    // Keeps rows 1–4 (title and column headers) visible while scrolling.
    private static SheetView FrozenHeaderView() => new(
        new Pane
        {
            VerticalSplit = HeaderRow,
            TopLeftCell = $"A{HeaderRow + 1}",
            ActivePane = PaneValues.BottomLeft,
            State = PaneStateValues.Frozen,
        },
        new Selection { Pane = PaneValues.BottomLeft })
    {
        WorkbookViewId = 0,
    };

    private static Stylesheet CreateStylesheet() => new(
        new Fonts(
            new Font(new FontSize { Val = 11 }, new FontName { Val = "Calibri" }),
            new Font(new Bold(), new FontSize { Val = 11 }, new FontName { Val = "Calibri" }),
            new Font(new Bold(), new FontSize { Val = 14 }, new FontName { Val = "Calibri" })),
        // The first two fills are required by Excel (none and gray125).
        new Fills(
            new Fill(new PatternFill { PatternType = PatternValues.None }),
            new Fill(new PatternFill { PatternType = PatternValues.Gray125 })),
        new Borders(new Border()),
        new CellFormats(
            new CellFormat { FontId = 0 },                                              // NormalStyle
            new CellFormat { FontId = 1, ApplyFont = true },                            // HeaderStyle
            new CellFormat { FontId = 0, NumberFormatId = 14, ApplyNumberFormat = true }, // DateStyle (built-in date format)
            new CellFormat { FontId = 2, ApplyFont = true }));                          // TitleStyle
}
