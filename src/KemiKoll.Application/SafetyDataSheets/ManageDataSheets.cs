namespace KemiKoll.Application.SafetyDataSheets;

/// <summary>
/// One safety data sheet of a product, ready to show on the edit page.
/// </summary>
/// <param name="Id">The data sheet id.</param>
/// <param name="RevisionDate">The revision date printed on the data sheet.</param>
/// <param name="LanguageCode">Two-letter language code, for example "sv".</param>
/// <param name="FileName">The original PDF file name.</param>
/// <param name="IsLatest">
/// <see langword="true"/> for the newest revision, which decides the product's data sheet status.
/// Older revisions are kept as history.
/// </param>
public sealed record DataSheetDetails(Guid Id, DateOnly RevisionDate, string LanguageCode, string FileName, bool IsLatest);

/// <summary>The corrected values of an existing data sheet.</summary>
/// <param name="Id">The data sheet id.</param>
/// <param name="RevisionDate">The correct revision date.</param>
/// <param name="LanguageCode">The correct two-letter language code.</param>
public sealed record CorrectDataSheetCommand(Guid Id, DateOnly RevisionDate, string LanguageCode);

/// <summary>The outcome of a correction: saved, or error messages.</summary>
/// <param name="Errors">Why the correction was not saved. Empty on success.</param>
public sealed record CorrectDataSheetResult(IReadOnlyList<string> Errors)
{
    /// <summary><see langword="true"/> if the correction was saved.</summary>
    public bool Succeeded => Errors.Count == 0;
}

/// <summary>
/// Use cases for the safety data sheets of one product on the edit page:
/// list them, correct a mistake, remove one added by mistake.
/// </summary>
/// <remarks>
/// A newer version from the supplier is added with <see cref="AddSafetyDataSheet"/> as a new data sheet,
/// so older versions stay as history.
/// </remarks>
/// <param name="sheets">Where safety data sheets are stored.</param>
public sealed class ManageDataSheets(ISafetyDataSheetRepository sheets)
{
    /// <summary>Lists a product's data sheets, newest revision first.</summary>
    /// <param name="productId">The product id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The data sheets; the first one is marked as the latest.</returns>
    public async Task<IReadOnlyList<DataSheetDetails>> ListAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var productSheets = await sheets.ListForProductAsync(productId, cancellationToken).ConfigureAwait(false);

        return productSheets
            .OrderByDescending(sheet => sheet.RevisionDate)
            .Select((sheet, index) => new DataSheetDetails(sheet.Id, sheet.RevisionDate, sheet.LanguageCode, sheet.FileName, IsLatest: index == 0))
            .ToList();
    }

    /// <summary>Corrects the revision date and language of a data sheet.</summary>
    /// <param name="command">The corrected values.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>Success, or readable error messages.</returns>
    public async Task<CorrectDataSheetResult> CorrectAsync(CorrectDataSheetCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sheet = await sheets.GetAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (sheet is null)
        {
            return new CorrectDataSheetResult(["The data sheet no longer exists."]);
        }

        try
        {
            sheet.Correct(command.RevisionDate, command.LanguageCode);
        }
        catch (ArgumentException exception)
        {
            return new CorrectDataSheetResult([UserMessages.From(exception)]);
        }

        var saved = await sheets.UpdateAsync(sheet, cancellationToken).ConfigureAwait(false);
        return new CorrectDataSheetResult(saved ? [] : ["The data sheet no longer exists."]);
    }

    /// <summary>Removes a data sheet, for example one attached by mistake.</summary>
    /// <param name="id">The data sheet id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns><see langword="true"/> if removed; <see langword="false"/> if it did not exist.</returns>
    public Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default) =>
        sheets.DeleteAsync(id, cancellationToken);
}
