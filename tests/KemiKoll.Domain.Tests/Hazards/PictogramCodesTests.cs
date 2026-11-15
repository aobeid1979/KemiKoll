using KemiKoll.Domain.Hazards;

namespace KemiKoll.Domain.Tests.Hazards;

public class PictogramCodesTests
{
    [Theory]
    [InlineData(Pictogram.Explosive, "GHS01")]
    [InlineData(Pictogram.Flammable, "GHS02")]
    [InlineData(Pictogram.Oxidising, "GHS03")]
    [InlineData(Pictogram.GasUnderPressure, "GHS04")]
    [InlineData(Pictogram.Corrosive, "GHS05")]
    [InlineData(Pictogram.AcuteToxicity, "GHS06")]
    [InlineData(Pictogram.HealthHazardLow, "GHS07")]
    [InlineData(Pictogram.SeriousHealthHazard, "GHS08")]
    [InlineData(Pictogram.Environment, "GHS09")]
    public void ToCode_And_Parse_MatchOfficialCodes(Pictogram pictogram, string code)
    {
        Assert.Equal(code, pictogram.ToCode());
        Assert.Equal(pictogram, PictogramCodes.Parse(code));
    }

    [Fact]
    public void AllNinePictograms_AreDefined()
    {
        Assert.Equal(9, Enum.GetValues<Pictogram>().Length);
    }

    [Theory]
    [InlineData("ghs02")]
    [InlineData("  GHS02  ")]
    [InlineData("Ghs02")]
    public void TryParse_IgnoresCaseAndSurroundingSpaces(string code)
    {
        Assert.True(PictogramCodes.TryParse(code, out var pictogram));
        Assert.Equal(Pictogram.Flammable, pictogram);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("GHS00")]
    [InlineData("GHS10")]
    [InlineData("GHS2")]
    [InlineData("GHS002")]
    [InlineData("GHSAB")]
    [InlineData("02")]
    [InlineData("GHS 02")]
    public void TryParse_InvalidCode_ReturnsFalse(string? code)
    {
        Assert.False(PictogramCodes.TryParse(code, out _));
    }

    [Fact]
    public void Parse_InvalidCode_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => PictogramCodes.Parse("GHS10"));
    }

    [Fact]
    public void ToCode_UndefinedValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((Pictogram)42).ToCode());
    }
}
