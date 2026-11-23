using KemiKoll.Application.Products;

namespace KemiKoll.Application.Exports;

/// <summary>
/// Writes the chemical inventory as an Excel file (.xlsx).
/// </summary>
/// <remarks>
/// The Application layer only knows this interface; the file format details
/// (Open XML) live in the Infrastructure layer.
/// </remarks>
public interface IInventorySpreadsheetWriter
{
    /// <summary>Writes one row per product, with a title and the export date above the table.</summary>
    /// <param name="products">The products, in the order they should appear.</param>
    /// <param name="exportDate">The date printed in the file.</param>
    /// <param name="output">The stream to write the .xlsx file to.</param>
    void Write(IReadOnlyList<ProductSummary> products, DateOnly exportDate, Stream output);
}
