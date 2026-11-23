using System.Globalization;

using KemiKoll.Application.Products;

namespace KemiKoll.Application.Exports;

/// <summary>
/// Use case: export the whole chemical inventory (kemikalieförteckning) as an Excel file,
/// to print, send or keep as a record.
/// </summary>
/// <param name="listProducts">Loads the products with their data sheet status.</param>
/// <param name="writer">Writes the Excel file.</param>
/// <param name="timeProvider">Gives today's date for the file name and the file itself.</param>
public sealed class ExportInventory(
    ListProducts listProducts,
    IInventorySpreadsheetWriter writer,
    TimeProvider timeProvider)
{
    /// <summary>The MIME type of .xlsx files.</summary>
    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Creates the Excel file with all products, sorted by name.</summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The file, named after today's date.</returns>
    public async Task<ExportedFile> ToExcelAsync(CancellationToken cancellationToken = default)
    {
        var products = await listProducts.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        using var output = new MemoryStream();
        writer.Write(products, today, output);

        var fileName = $"kemikoll-inventory-{today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.xlsx";
        return new ExportedFile(fileName, ExcelContentType, output.ToArray());
    }
}
