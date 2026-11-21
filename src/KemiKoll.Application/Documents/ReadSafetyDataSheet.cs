namespace KemiKoll.Application.Documents;

/// <summary>
/// The outcome of reading an uploaded safety data sheet: either the extracted
/// suggestions, or an error message the user can understand.
/// </summary>
public sealed record ReadSafetyDataSheetResult
{
    private ReadSafetyDataSheetResult(SafetyDataSheetExtraction? extraction, string? error)
    {
        Extraction = extraction;
        Error = error;
    }

    /// <summary>The suggestions found in the data sheet, or <see langword="null"/> on error.</summary>
    public SafetyDataSheetExtraction? Extraction { get; }

    /// <summary>Why the file could not be read, or <see langword="null"/> on success.</summary>
    public string? Error { get; }

    /// <summary><see langword="true"/> if the file was read.</summary>
    public bool Succeeded => Extraction is not null;

    internal static ReadSafetyDataSheetResult Success(SafetyDataSheetExtraction extraction) => new(extraction, null);

    internal static ReadSafetyDataSheetResult Failure(string error) => new(null, error);
}

/// <summary>
/// Use case: read an uploaded safety data sheet PDF and suggest the product details.
/// </summary>
/// <remarks>
/// Nothing is saved here. The suggestions fill the "Add product" form, where the user
/// checks and confirms them.
/// </remarks>
/// <param name="pdfReader">Reads the text of the PDF.</param>
public sealed class ReadSafetyDataSheet(IPdfTextReader pdfReader)
{
    /// <summary>Reads the PDF and extracts the suggestions.</summary>
    /// <param name="pdf">The uploaded PDF file.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The suggestions, or an error message.</returns>
    public async Task<ReadSafetyDataSheetResult> ExecuteAsync(Stream pdf, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdf);

        PdfText text;
        try
        {
            text = await pdfReader.ReadAsync(pdf, cancellationToken).ConfigureAwait(false);
        }
        catch (PdfReadException exception)
        {
            return ReadSafetyDataSheetResult.Failure(exception.Message);
        }

        if (!text.HasText)
        {
            return ReadSafetyDataSheetResult.Failure(
                "The PDF contains no text; it is probably a scan. Ask the supplier for a digital version, or enter the details by hand.");
        }

        return ReadSafetyDataSheetResult.Success(SafetyDataSheetTextParser.Parse(text.FullText));
    }
}
