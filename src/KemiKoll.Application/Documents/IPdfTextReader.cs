namespace KemiKoll.Application.Documents;

/// <summary>
/// Reads the text out of a PDF file, for example an uploaded safety data sheet.
/// </summary>
/// <remarks>
/// The implementation lives in the Infrastructure layer, so the PDF library can be
/// replaced without changing any use case.
/// </remarks>
public interface IPdfTextReader
{
    /// <summary>Reads the text of every page.</summary>
    /// <param name="pdf">The PDF file content. Read from the current position; not closed.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The text, one entry per page.</returns>
    /// <exception cref="PdfReadException">The file is not a readable PDF, is password-protected, or is too large.</exception>
    Task<PdfText> ReadAsync(Stream pdf, CancellationToken cancellationToken = default);
}

/// <summary>
/// The text of a PDF file, one entry per page.
/// </summary>
/// <param name="Pages">The text of each page, in page order. A page without text is an empty string.</param>
public sealed record PdfText(IReadOnlyList<string> Pages)
{
    /// <summary>All pages joined, separated by a blank line.</summary>
    public string FullText => string.Join("\n\n", Pages);

    /// <summary>
    /// <see langword="false"/> when no page contains text. This usually means the PDF is a scan
    /// (only images), which needs OCR before its content can be read.
    /// </summary>
    public bool HasText => Pages.Any(page => !string.IsNullOrWhiteSpace(page));
}

/// <summary>
/// Thrown when a file cannot be read as a PDF. The message can be shown to the user.
/// </summary>
public sealed class PdfReadException : Exception
{
    /// <summary>Creates the exception with a default message.</summary>
    public PdfReadException()
        : base("The file could not be read as a PDF.")
    {
    }

    /// <summary>Creates the exception with a message for the user.</summary>
    /// <param name="message">Explains why the file could not be read.</param>
    public PdfReadException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message for the user and the original error.</summary>
    /// <param name="message">Explains why the file could not be read.</param>
    /// <param name="innerException">The error from the PDF library.</param>
    public PdfReadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
