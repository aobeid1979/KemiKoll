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
    /// <summary>Writes one row per product, with a title, the export date and the selection above the table.</summary>
    /// <param name="sheet">The products and the lines above the table.</param>
    /// <param name="output">The stream to write the .xlsx file to.</param>
    void Write(InventorySheet sheet, Stream output);
}
