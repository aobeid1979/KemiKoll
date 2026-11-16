using KemiKoll.Domain.Companies;

namespace KemiKoll.Domain.Tests.Companies;

public class OrganizationNumberTests
{
    [Theory]
    [InlineData("556036-0793", "556036-0793")]
    [InlineData("5560360793", "556036-0793")]
    [InlineData("  212000-0142  ", "212000-0142")]
    [InlineData("802479-3781", "802479-3781")]
    public void TryParse_ValidNumber_ReturnsStandardFormat(string input, string expected)
    {
        Assert.True(OrganizationNumber.TryParse(input, out var number));
        Assert.Equal(expected, number.Value);
    }

    [Fact]
    public void TryParse_SoleTraderPersonalNumberFormat_IsAccepted()
    {
        // A sole proprietorship uses the owner's personal identity number (YYMMDD-NNNN).
        Assert.True(OrganizationNumber.TryParse("811218-9876", out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("556036-0794")] // wrong check digit
    [InlineData("556036079")]   // too short
    [InlineData("55603607931")] // too long
    [InlineData("5560-360793")] // hyphen in the wrong place
    [InlineData("556036 0793")] // space instead of hyphen
    [InlineData("55603A-0793")] // letter
    [InlineData("556036-079３")] // non-ASCII digit
    public void TryParse_InvalidNumber_ReturnsFalse(string? input)
    {
        Assert.False(OrganizationNumber.TryParse(input, out var number));
        Assert.Null(number);
    }

    [Fact]
    public void Parse_InvalidNumber_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => OrganizationNumber.Parse("556036-0794"));
    }

    [Fact]
    public void Numbers_WrittenDifferently_AreEqual()
    {
        Assert.Equal(OrganizationNumber.Parse("5560360793"), OrganizationNumber.Parse("556036-0793"));
    }

    [Fact]
    public void ToString_ReturnsStandardFormat()
    {
        Assert.Equal("556036-0793", OrganizationNumber.Parse("5560360793").ToString());
    }
}
