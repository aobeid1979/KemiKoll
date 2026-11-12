using KemiKoll.Domain.Hazards;

namespace KemiKoll.Domain.Tests.Hazards;

public class HazardStatementTests
{
    [Theory]
    [InlineData("H225", HazardCategory.Physical)]
    [InlineData("H290", HazardCategory.Physical)]
    [InlineData("H315", HazardCategory.Health)]
    [InlineData("H350i", HazardCategory.Health)]
    [InlineData("H360FD", HazardCategory.Health)]
    [InlineData("H361d", HazardCategory.Health)]
    [InlineData("H410", HazardCategory.Environmental)]
    [InlineData("EUH066", HazardCategory.SupplementalEu)]
    [InlineData("EUH201A", HazardCategory.SupplementalEu)]
    public void Parse_ValidCode_ReturnsStatementWithCategory(string code, HazardCategory expectedCategory)
    {
        var statement = HazardStatement.Parse(code);

        Assert.Equal(code, statement.Code);
        Assert.Equal(expectedCategory, statement.Category);
    }

    [Theory]
    [InlineData("h225", "H225")]
    [InlineData("  H225  ", "H225")]
    [InlineData("euh066", "EUH066")]
    [InlineData("EUH201a", "EUH201A")]
    public void Parse_NormalizesPrefixAndWhitespace(string input, string expectedCode)
    {
        var statement = HazardStatement.Parse(input);

        Assert.Equal(expectedCode, statement.Code);
    }

    [Fact]
    public void Parse_KeepsCaseOfReproductiveToxicitySuffix()
    {
        // H360FD = may damage fertility and the unborn child.
        // H360Fd = may damage fertility, suspected of damaging the unborn child.
        var mayDamageBoth = HazardStatement.Parse("H360FD");
        var mixed = HazardStatement.Parse("H360Fd");

        Assert.Equal("H360Fd", mixed.Code);
        Assert.NotEqual(mayDamageBoth, mixed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("225")]
    [InlineData("H")]
    [InlineData("H22")]
    [InlineData("H2250")]
    [InlineData("H125")]
    [InlineData("H525")]
    [InlineData("P210")]
    [InlineData("H225X")]
    [InlineData("H360FDF")]
    [InlineData("EUH66")]
    [InlineData("EUH066B")]
    [InlineData("H 225")]
    public void TryParse_InvalidCode_ReturnsFalse(string code)
    {
        var success = HazardStatement.TryParse(code, out var statement);

        Assert.False(success);
        Assert.Null(statement);
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        Assert.False(HazardStatement.TryParse(null, out _));
    }

    [Fact]
    public void Parse_InvalidCode_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => HazardStatement.Parse("P210"));
    }

    [Fact]
    public void Parse_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => HazardStatement.Parse(null!));
    }

    [Fact]
    public void Statements_WithSameCode_AreEqual()
    {
        Assert.Equal(HazardStatement.Parse("h225"), HazardStatement.Parse("H225"));
    }

    [Fact]
    public void ToString_ReturnsCode()
    {
        Assert.Equal("H225", HazardStatement.Parse("H225").ToString());
    }
}
