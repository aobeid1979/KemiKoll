using KemiKoll.Domain.Hazards;

namespace KemiKoll.Domain.Tests.Hazards;

public class PictogramCodesTests
{
    [Theory]
    [InlineData(Pictogram.ExplodingBomb, "GHS01")]
    [InlineData(Pictogram.Flame, "GHS02")]
    [InlineData(Pictogram.FlameOverCircle, "GHS03")]
    [InlineData(Pictogram.GasCylinder, "GHS04")]
    [InlineData(Pictogram.Corrosion, "GHS05")]
    [InlineData(Pictogram.SkullAndCrossbones, "GHS06")]
    [InlineData(Pictogram.ExclamationMark, "GHS07")]
    [InlineData(Pictogram.HealthHazard, "GHS08")]
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
        Assert.Equal(Pictogram.Flame, pictogram);
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
