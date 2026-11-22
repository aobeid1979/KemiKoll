using KemiKoll.Application.SafetyDataSheets;
using KemiKoll.Application.Tests.TestDoubles;
using KemiKoll.Domain.SafetyDataSheets;

namespace KemiKoll.Application.Tests.SafetyDataSheets;

public class ManageDataSheetsTests
{
    private static readonly Guid ProductId = Guid.NewGuid();

    private readonly FakeSafetyDataSheetRepository _sheets = new();
    private readonly ManageDataSheets _manage;

    public ManageDataSheetsTests() => _manage = new ManageDataSheets(_sheets);

    private SafetyDataSheet AddSheet(DateOnly revisionDate, Guid? productId = null, string languageCode = "sv")
    {
        var sheet = SafetyDataSheet.Create(productId ?? ProductId, revisionDate, languageCode, "sheet.pdf");
        _sheets.Sheets.Add(sheet);
        return sheet;
    }

    [Fact]
    public async Task ListAsync_ReturnsProductSheetsNewestFirstAndMarksLatest()
    {
        AddSheet(new DateOnly(2019, 1, 1));
        AddSheet(new DateOnly(2025, 8, 7));
        AddSheet(new DateOnly(2022, 6, 1));
        AddSheet(new DateOnly(2026, 1, 1), productId: Guid.NewGuid()); // another product

        var result = await _manage.ListAsync(ProductId);

        DateOnly[] expected = [new(2025, 8, 7), new(2022, 6, 1), new(2019, 1, 1)];
        Assert.Equal(expected, result.Select(s => s.RevisionDate));
        bool[] expectedLatest = [true, false, false];
        Assert.Equal(expectedLatest, result.Select(s => s.IsLatest));
    }

    [Fact]
    public async Task ListAsync_NoSheets_ReturnsEmpty()
    {
        Assert.Empty(await _manage.ListAsync(ProductId));
    }

    [Fact]
    public async Task CorrectAsync_ValidValues_CorrectsAndSaves()
    {
        var sheet = AddSheet(new DateOnly(2019, 1, 16), languageCode: "sv");

        var result = await _manage.CorrectAsync(new CorrectDataSheetCommand(sheet.Id, new DateOnly(2019, 1, 17), "en"));

        Assert.True(result.Succeeded);
        Assert.Equal(1, _sheets.UpdateCount);
        Assert.Equal(new DateOnly(2019, 1, 17), sheet.RevisionDate);
        Assert.Equal("en", sheet.LanguageCode);
    }

    [Fact]
    public async Task CorrectAsync_InvalidLanguage_ReturnsReadableErrorAndChangesNothing()
    {
        var sheet = AddSheet(new DateOnly(2019, 1, 16));

        var result = await _manage.CorrectAsync(new CorrectDataSheetCommand(sheet.Id, new DateOnly(2020, 1, 1), "xyz"));

        Assert.False(result.Succeeded);
        Assert.StartsWith("LanguageCode:", Assert.Single(result.Errors), StringComparison.Ordinal);
        Assert.Equal(new DateOnly(2019, 1, 16), sheet.RevisionDate);
        Assert.Equal(0, _sheets.UpdateCount);
    }

    [Fact]
    public async Task CorrectAsync_UnknownSheet_ReturnsError()
    {
        var result = await _manage.CorrectAsync(new CorrectDataSheetCommand(Guid.NewGuid(), new DateOnly(2020, 1, 1), "sv"));

        Assert.Equal("The data sheet no longer exists.", Assert.Single(result.Errors));
    }

    [Fact]
    public async Task RemoveAsync_RemovesOnlyThatSheet()
    {
        var mistake = AddSheet(new DateOnly(2025, 8, 7));
        var keep = AddSheet(new DateOnly(2022, 6, 1));

        Assert.True(await _manage.RemoveAsync(mistake.Id));
        Assert.Equal(keep, Assert.Single(_sheets.Sheets));
        Assert.False(await _manage.RemoveAsync(mistake.Id));
    }
}
