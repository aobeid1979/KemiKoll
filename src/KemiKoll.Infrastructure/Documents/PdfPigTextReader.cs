using KemiKoll.Application.Documents;

using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Exceptions;

namespace KemiKoll.Infrastructure.Documents;

/// <summary>
/// Reads PDF text with the PdfPig library.
/// </summary>
/// <remarks>
/// Uploaded files come from users, so they are treated as untrusted:
/// size and page count are limited before and during reading.
/// </remarks>
public sealed class PdfPigTextReader : IPdfTextReader
{
    /// <summary>The largest file accepted: 20 MB. Safety data sheets are usually well below 2 MB.</summary>
    public const long MaxFileSizeInBytes = 20 * 1024 * 1024;

    /// <summary>The most pages read. Safety data sheets usually have 10–30 pages.</summary>
    public const int MaxPages = 100;

    /// <inheritdoc />
    public async Task<PdfText> ReadAsync(Stream pdf, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdf);

        var bytes = await ReadLimitedAsync(pdf, cancellationToken).ConfigureAwait(false);

        try
        {
            using var document = PdfDocument.Open(bytes);

            if (document.NumberOfPages > MaxPages)
            {
                throw new PdfReadException($"The PDF has {document.NumberOfPages} pages; at most {MaxPages} are supported.");
            }

            var pages = new List<string>(document.NumberOfPages);
            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // ContentOrderTextExtractor keeps the reading order and line breaks,
                // which matters for finding headings such as "SECTION 2: Hazards identification".
                pages.Add(ContentOrderTextExtractor.GetText(page));
            }

            return new PdfText(pages);
        }
        catch (PdfDocumentEncryptedException exception)
        {
            throw new PdfReadException("The PDF is password-protected. Upload a version without a password.", exception);
        }
        catch (PdfDocumentFormatException exception)
        {
            throw new PdfReadException("The file is not a valid PDF.", exception);
        }
        catch (Exception exception) when (exception is not PdfReadException and not OperationCanceledException)
        {
            // Damaged or unusual files can make the library fail in other ways;
            // the user only needs to know that this file cannot be read.
            throw new PdfReadException("The file is not a valid PDF.", exception);
        }
    }

    // Copies the stream into memory, but never more than MaxFileSizeInBytes,
    // even if the stream does not report its length.
    private static async Task<byte[]> ReadLimitedAsync(Stream pdf, CancellationToken cancellationToken)
    {
        if (pdf.CanSeek && pdf.Length - pdf.Position > MaxFileSizeInBytes)
        {
            throw new PdfReadException(TooLargeMessage);
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await pdf.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > MaxFileSizeInBytes)
            {
                throw new PdfReadException(TooLargeMessage);
            }

            buffer.Write(chunk, 0, read);
        }

        if (buffer.Length == 0)
        {
            throw new PdfReadException("The file is empty.");
        }

        return buffer.ToArray();
    }

    private static string TooLargeMessage => $"The file is larger than {MaxFileSizeInBytes / (1024 * 1024)} MB.";
}
