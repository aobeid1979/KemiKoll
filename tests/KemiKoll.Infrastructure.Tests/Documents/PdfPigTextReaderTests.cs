using System.Globalization;
using System.Text;

using KemiKoll.Application.Documents;
using KemiKoll.Infrastructure.Documents;

using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace KemiKoll.Infrastructure.Tests.Documents;

public class PdfPigTextReaderTests
{
    private readonly PdfPigTextReader _reader = new();

    /// <summary>Builds a small PDF in memory: one page per entry, each line written below the previous one.</summary>
    private static MemoryStream CreatePdf(params string[][] pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        foreach (var lines in pages)
        {
            var page = builder.AddPage(PageSize.A4);
            var y = 780.0;
            foreach (var line in lines)
            {
                page.AddText(line, 11, new PdfPoint(50, y), font);
                y -= 20;
            }
        }

        return new MemoryStream(builder.Build());
    }

    [Fact]
    public async Task ReadAsync_ReturnsTextOfEachPage()
    {
        using var pdf = CreatePdf(
            ["SECTION 1: Identification", "Product name: Acetone"],
            ["SECTION 2: Hazards identification", "H225 Highly flammable liquid and vapour"]);

        var text = await _reader.ReadAsync(pdf);

        Assert.Equal(2, text.Pages.Count);
        Assert.Contains("Product name: Acetone", text.Pages[0], StringComparison.Ordinal);
        Assert.Contains("H225 Highly flammable liquid and vapour", text.Pages[1], StringComparison.Ordinal);
        Assert.True(text.HasText);
    }

    [Fact]
    public async Task ReadAsync_KeepsLinesInReadingOrder()
    {
        using var pdf = CreatePdf(["First line", "Second line", "Third line"]);

        var page = (await _reader.ReadAsync(pdf)).Pages[0];

        var first = page.IndexOf("First line", StringComparison.Ordinal);
        var second = page.IndexOf("Second line", StringComparison.Ordinal);
        var third = page.IndexOf("Third line", StringComparison.Ordinal);
        Assert.True(first >= 0 && first < second && second < third, page);
    }

    /// <summary>
    /// Writes a one-page PDF by hand with the WinAnsi (Western European) encoding, which many
    /// Swedish PDFs use for å, ä and ö. PdfPig's own builder cannot write these letters with its
    /// built-in fonts, so this small writer is used for the Swedish-character test only.
    /// </summary>
    private static MemoryStream CreateWinAnsiPdf(params string[] lines)
    {
        static string Escape(string text) =>
            text.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);

        var content = new StringBuilder("BT /F1 11 Tf 50 780 Td");
        foreach (var line in lines)
        {
            content.Append(CultureInfo.InvariantCulture, $" ({Escape(line)}) Tj 0 -20 Td");
        }

        content.Append(" ET");
        var contentBytes = Encoding.Latin1.GetBytes(content.ToString());

        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            $"<< /Length {contentBytes.Length} >>\nstream\n{Encoding.Latin1.GetString(contentBytes)}\nendstream",
        ];

        // A PDF ends with a table of the byte position of every object (xref).
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(pdf.Length);
            pdf.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xrefOffset = pdf.Length;
        pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            pdf.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        pdf.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");

        // Latin-1 writes one byte per character, so the string positions above are byte positions.
        return new MemoryStream(Encoding.Latin1.GetBytes(pdf.ToString()));
    }

    [Fact]
    public async Task ReadAsync_ReadsSwedishCharacters()
    {
        using var pdf = CreateWinAnsiPdf("Säkerhetsdatablad - Avsnitt 2: Farliga egenskaper", "Leverantör: Exempel Kemi AB, Åmål");

        var text = await _reader.ReadAsync(pdf);

        Assert.Contains("Säkerhetsdatablad", text.FullText, StringComparison.Ordinal);
        Assert.Contains("Leverantör", text.FullText, StringComparison.Ordinal);
        Assert.Contains("Åmål", text.FullText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadAsync_PageWithoutText_HasNoText()
    {
        // A scanned data sheet contains only images, so no text can be read.
        using var pdf = CreatePdf(Array.Empty<string>());

        var text = await _reader.ReadAsync(pdf);

        Assert.Single(text.Pages);
        Assert.False(text.HasText);
    }

    [Fact]
    public async Task ReadAsync_NotAPdf_ThrowsReadableError()
    {
        using var notPdf = new MemoryStream(Encoding.UTF8.GetBytes("This is a text file, not a PDF."));

        var exception = await Assert.ThrowsAsync<PdfReadException>(() => _reader.ReadAsync(notPdf));

        Assert.Equal("The file is not a valid PDF.", exception.Message);
    }

    [Fact]
    public async Task ReadAsync_EmptyFile_ThrowsReadableError()
    {
        using var empty = new MemoryStream();

        var exception = await Assert.ThrowsAsync<PdfReadException>(() => _reader.ReadAsync(empty));

        Assert.Equal("The file is empty.", exception.Message);
    }

    [Fact]
    public async Task ReadAsync_FileTooLarge_IsRejectedBeforeReading()
    {
        using var large = new MemoryStream(new byte[PdfPigTextReader.MaxFileSizeInBytes + 1]);

        var exception = await Assert.ThrowsAsync<PdfReadException>(() => _reader.ReadAsync(large));

        Assert.Contains("larger than 20 MB", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadAsync_TooManyPages_IsRejected()
    {
        var pages = Enumerable.Range(1, PdfPigTextReader.MaxPages + 1).Select(n => new[] { $"Page {n}" }).ToArray();
        using var pdf = CreatePdf(pages);

        var exception = await Assert.ThrowsAsync<PdfReadException>(() => _reader.ReadAsync(pdf));

        Assert.Contains("at most 100", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FullText_JoinsPagesWithBlankLine()
    {
        var text = new PdfText(["Page one", "Page two"]);

        Assert.Equal("Page one\n\nPage two", text.FullText);
    }
}
