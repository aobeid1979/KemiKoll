using KemiKoll.Application.Documents;

namespace KemiKoll.Application.Tests.Documents;

public class ReadSafetyDataSheetTests
{
    /// <summary>Returns fixed pages or throws, instead of reading a real PDF.</summary>
    private sealed class FakePdfTextReader(PdfText? text = null, PdfReadException? error = null) : IPdfTextReader
    {
        public Task<PdfText> ReadAsync(Stream pdf, CancellationToken cancellationToken = default) =>
            error is not null ? Task.FromException<PdfText>(error) : Task.FromResult(text!);
    }

    [Fact]
    public async Task ExecuteAsync_ReadablePdf_ReturnsSuggestions()
    {
        var reader = new FakePdfTextReader(new PdfText(
        [
            "Product name: Acetone\nSECTION 2: Hazards identification\nH225",
            "SECTION 3: Composition",
        ]));
        using var file = new MemoryStream();

        var result = await new ReadSafetyDataSheet(reader).ExecuteAsync(file);

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Equal("Acetone", result.Extraction!.ProductName);
        Assert.Equal("H225", Assert.Single(result.Extraction.HazardCodes));
    }

    [Fact]
    public async Task ExecuteAsync_ScannedPdf_ReturnsClearError()
    {
        var reader = new FakePdfTextReader(new PdfText(["", "  "]));
        using var file = new MemoryStream();

        var result = await new ReadSafetyDataSheet(reader).ExecuteAsync(file);

        Assert.False(result.Succeeded);
        Assert.Contains("scan", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_UnreadableFile_ReturnsReaderMessage()
    {
        var reader = new FakePdfTextReader(error: new PdfReadException("The file is not a valid PDF."));
        using var file = new MemoryStream();

        var result = await new ReadSafetyDataSheet(reader).ExecuteAsync(file);

        Assert.False(result.Succeeded);
        Assert.Equal("The file is not a valid PDF.", result.Error);
    }
}
