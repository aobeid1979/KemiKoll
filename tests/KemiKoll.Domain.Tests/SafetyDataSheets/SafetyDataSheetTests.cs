using KemiKoll.Domain.SafetyDataSheets;

namespace KemiKoll.Domain.Tests.SafetyDataSheets;

public class SafetyDataSheetTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly DateOnly RevisionDate = new(2024, 3, 15);

    private static SafetyDataSheet CreateSheet(string languageCode = "sv", string fileName = "degreaser.pdf") =>
        SafetyDataSheet.Create(ProductId, RevisionDate, languageCode, fileName);

    [Fact]
    public void Create_ValidInput_SetsProperties()
    {
        var sheet = SafetyDataSheet.Create(ProductId, RevisionDate, " SV ", " degreaser.pdf ");

        Assert.NotEqual(Guid.Empty, sheet.Id);
        Assert.Equal(ProductId, sheet.ProductId);
        Assert.Equal(RevisionDate, sheet.RevisionDate);
        Assert.Equal("sv", sheet.LanguageCode);
        Assert.Equal("degreaser.pdf", sheet.FileName);
        Assert.True(sheet.IsInSwedish);
    }

    [Fact]
    public void IsInSwedish_EnglishSheet_ReturnsFalse()
    {
        Assert.False(CreateSheet(languageCode: "en").IsInSwedish);
    }

    [Fact]
    public void Create_EmptyProductId_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => SafetyDataSheet.Create(Guid.Empty, RevisionDate, "sv", "a.pdf"));
        Assert.Equal("productId", exception.ParamName);
    }

    [Fact]
    public void Create_DefaultRevisionDate_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => SafetyDataSheet.Create(ProductId, default, "sv", "a.pdf"));
        Assert.Equal("revisionDate", exception.ParamName);
    }

    [Theory]
    [InlineData("swe")]
    [InlineData("s")]
    [InlineData("s1")]
    [InlineData("sv-SE")]
    public void Create_InvalidLanguageCode_Throws(string languageCode)
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateSheet(languageCode: languageCode));
        Assert.Equal("languageCode", exception.ParamName);
    }

    [Theory]
    [InlineData("sheet.docx")]
    [InlineData("sheet")]
    [InlineData("sheet.pdf.exe")]
    public void Create_NonPdfFile_Throws(string fileName)
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateSheet(fileName: fileName));
        Assert.Equal("fileName", exception.ParamName);
    }

    [Fact]
    public void Create_UpperCasePdfExtension_IsAllowed()
    {
        Assert.Equal("SHEET.PDF", CreateSheet(fileName: "SHEET.PDF").FileName);
    }

    [Fact]
    public void Create_FileNameTooLong_Throws()
    {
        var fileName = new string('a', SafetyDataSheet.MaxFileNameLength - 3) + ".pdf";

        Assert.Throws<ArgumentException>(() => CreateSheet(fileName: fileName));
    }

    [Fact]
    public void ReviewDueDate_DefaultPolicy_IsThreeYearsAfterRevision()
    {
        Assert.Equal(new DateOnly(2027, 3, 15), CreateSheet().ReviewDueDate(ReviewPolicy.Default));
    }

    [Fact]
    public void ReviewDueDate_CustomPolicy_UsesCompanyChoice()
    {
        Assert.Equal(new DateOnly(2029, 3, 15), CreateSheet().ReviewDueDate(ReviewPolicy.Create(5)));
    }

    [Theory]
    [InlineData(2027, 3, 14, false)] // one day before the due date
    [InlineData(2027, 3, 15, true)]  // on the due date
    [InlineData(2028, 1, 1, true)]   // after the due date
    public void IsDueForReview_DefaultPolicy_DependsOnDate(int year, int month, int day, bool expected)
    {
        var today = new DateOnly(year, month, day);

        Assert.Equal(expected, CreateSheet().IsDueForReview(today, ReviewPolicy.Default));
    }

    [Fact]
    public void IsDueForReview_NullPolicy_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CreateSheet().IsDueForReview(new DateOnly(2027, 1, 1), null!));
    }
}
