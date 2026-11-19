using KemiKoll.Application.Products;
using KemiKoll.Domain.SafetyDataSheets;

namespace KemiKoll.Application.SafetyDataSheets;

/// <summary>
/// The data needed to register a safety data sheet for a product.
/// </summary>
/// <param name="ProductId">The product the data sheet belongs to.</param>
/// <param name="RevisionDate">The revision date printed on the data sheet.</param>
/// <param name="LanguageCode">Two-letter language code, for example "sv".</param>
/// <param name="FileName">The original PDF file name.</param>
public sealed record AddSafetyDataSheetCommand(
    Guid ProductId,
    DateOnly RevisionDate,
    string LanguageCode,
    string FileName);

/// <summary>
/// The outcome of registering a safety data sheet: the new id, or error messages.
/// </summary>
public sealed record AddSafetyDataSheetResult
{
    private AddSafetyDataSheetResult(Guid? sheetId, IReadOnlyList<string> errors)
    {
        SheetId = sheetId;
        Errors = errors;
    }

    /// <summary>The id of the new data sheet, or <see langword="null"/> if it was not added.</summary>
    public Guid? SheetId { get; }

    /// <summary>Messages explaining why the data sheet was not added. Empty on success.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary><see langword="true"/> if the data sheet was added.</summary>
    public bool Succeeded => SheetId is not null;

    internal static AddSafetyDataSheetResult Success(Guid sheetId) => new(sheetId, []);

    internal static AddSafetyDataSheetResult Failure(string error) => new(null, [error]);
}

/// <summary>
/// Use case: register a safety data sheet for an existing product.
/// </summary>
/// <remarks>
/// Only the facts about the data sheet are stored for now. Uploading and storing
/// the PDF file itself is added together with the AI extraction.
/// </remarks>
/// <param name="products">Used to check that the product exists.</param>
/// <param name="sheets">Where data sheets are stored.</param>
public sealed class AddSafetyDataSheet(IProductRepository products, ISafetyDataSheetRepository sheets)
{
    /// <summary>Validates the input, creates the data sheet and saves it.</summary>
    /// <param name="command">The data sheet details.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The new data sheet id, or the validation error.</returns>
    public async Task<AddSafetyDataSheetResult> ExecuteAsync(
        AddSafetyDataSheetCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var allProducts = await products.ListAsync(cancellationToken).ConfigureAwait(false);
        if (!allProducts.Any(product => product.Id == command.ProductId))
        {
            return AddSafetyDataSheetResult.Failure("The product does not exist.");
        }

        SafetyDataSheet sheet;
        try
        {
            sheet = SafetyDataSheet.Create(command.ProductId, command.RevisionDate, command.LanguageCode, command.FileName);
        }
        catch (ArgumentException exception)
        {
            return AddSafetyDataSheetResult.Failure(UserMessages.From(exception));
        }

        await sheets.AddAsync(sheet, cancellationToken).ConfigureAwait(false);
        return AddSafetyDataSheetResult.Success(sheet.Id);
    }
}
