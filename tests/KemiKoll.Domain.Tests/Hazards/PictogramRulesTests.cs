using KemiKoll.Domain.Hazards;

namespace KemiKoll.Domain.Tests.Hazards;

public class PictogramRulesTests
{
    private static PictogramRequirement For(params string[] codes) =>
        PictogramRules.For(codes.Select(HazardStatement.Parse));

    private static string[] Codes(IEnumerable<Pictogram> pictograms) =>
        pictograms.Select(p => p.ToCode()).ToArray();

    private static void AssertCodes(IEnumerable<Pictogram> actual, params string[] expected) =>
        Assert.Equal(expected, Codes(actual));

    [Theory]
    [InlineData("H200", "GHS01")]
    [InlineData("H220", "GHS02")]
    [InlineData("H225", "GHS02")]
    [InlineData("H241", "GHS01,GHS02")]
    [InlineData("H271", "GHS03")]
    [InlineData("H280", "GHS04")]
    [InlineData("H290", "GHS05")]
    [InlineData("H314", "GHS05")]
    [InlineData("H318", "GHS05")]
    [InlineData("H301", "GHS06")]
    [InlineData("H330", "GHS06")]
    [InlineData("H302", "GHS07")]
    [InlineData("H315", "GHS07")]
    [InlineData("H336", "GHS07")]
    [InlineData("H420", "GHS07")]
    [InlineData("H304", "GHS08")]
    [InlineData("H334", "GHS08")]
    [InlineData("H350", "GHS08")]
    [InlineData("H373", "GHS08")]
    [InlineData("H400", "GHS09")]
    [InlineData("H411", "GHS09")]
    public void For_SingleCode_RequiresCorrectPictogram(string code, string expected)
    {
        AssertCodes(For(code).Required, expected.Split(','));
    }

    [Theory]
    [InlineData("H221")] // flammable gas, category 2
    [InlineData("H229")] // aerosol, category 3
    [InlineData("H362")] // lactation
    [InlineData("H412")] // aquatic chronic 3
    [InlineData("H413")] // aquatic chronic 4
    [InlineData("EUH066")] // supplemental EU statement
    public void For_CodeWithoutPictogram_RequiresNone(string code)
    {
        var requirement = For(code);

        Assert.Empty(requirement.Required);
        Assert.Empty(requirement.CodesWithoutRule);
    }

    [Theory]
    [InlineData("H360FD")]
    [InlineData("H360Fd")]
    [InlineData("H361d")]
    [InlineData("H350i")]
    public void For_CodeWithSuffix_UsesRuleOfBaseCode(string code)
    {
        AssertCodes(For(code).Required, "GHS08");
    }

    [Fact]
    public void For_Petrol_RequiresFlameExclamationHealthAndEnvironment()
    {
        // Hazard statements of a real Swedish petrol (bensin) data sheet.
        AssertCodes(For("H225", "H315", "H336", "H304", "H411").Required, "GHS02", "GHS07", "GHS08", "GHS09");
    }

    [Fact]
    public void For_SkullAndCrossbones_RemovesExclamationMark()
    {
        // Methanol: GHS06 applies, so GHS07 must not appear (CLP Article 26).
        AssertCodes(For("H225", "H301", "H311", "H331", "H370", "H302").Required, "GHS02", "GHS06", "GHS08");
    }

    [Fact]
    public void For_Corrosion_RemovesExclamationMarkForIrritation()
    {
        AssertCodes(For("H314", "H315", "H319").Required, "GHS05");
    }

    [Fact]
    public void For_Corrosion_KeepsExclamationMarkForOtherReasons()
    {
        AssertCodes(For("H314", "H302").Required, "GHS05", "GHS07");
    }

    [Fact]
    public void For_RespiratorySensitisation_RemovesExclamationMarkForSkinSensitisationAndIrritation()
    {
        AssertCodes(For("H334", "H317", "H315", "H319").Required, "GHS08");
    }

    [Fact]
    public void For_RespiratorySensitisation_KeepsExclamationMarkForOtherReasons()
    {
        AssertCodes(For("H334", "H317", "H335").Required, "GHS07", "GHS08");
    }

    [Fact]
    public void For_RequiredPictograms_AreInOfficialOrder()
    {
        AssertCodes(For("H410", "H314", "H226").Required, "GHS02", "GHS05", "GHS09");
    }

    [Fact]
    public void For_CodeWithoutKnownRule_IsReportedNotGuessed()
    {
        var requirement = For("H205", "H225");

        AssertCodes(requirement.Required, "GHS02");
        Assert.Equal("H205", Assert.Single(requirement.CodesWithoutRule));
    }

    [Fact]
    public void MissingAndUnexpected_EthanolWithWrongPictogram()
    {
        // H225 needs GHS02 (flame), not GHS03 (flame over circle, for oxidisers).
        var requirement = For("H225");

        AssertCodes(requirement.MissingFrom([Pictogram.FlameOverCircle]), "GHS02");
        AssertCodes(requirement.UnexpectedIn([Pictogram.FlameOverCircle]), "GHS03");
    }

    [Fact]
    public void MissingAndUnexpected_MatchingPictograms_AreEmpty()
    {
        var requirement = For("H225", "H319");

        Assert.Empty(requirement.MissingFrom([Pictogram.Flame, Pictogram.ExclamationMark]));
        Assert.Empty(requirement.UnexpectedIn([Pictogram.Flame, Pictogram.ExclamationMark]));
    }

    [Fact]
    public void UnexpectedIn_WhenSomeCodeHasNoRule_ReportsNothing()
    {
        // The unknown code could be the reason for the extra pictogram.
        var requirement = For("H205");

        Assert.Empty(requirement.UnexpectedIn([Pictogram.ExplodingBomb]));
    }

    [Fact]
    public void For_NoStatements_RequiresNothing()
    {
        Assert.Empty(PictogramRules.For([]).Required);
    }
}
