using KemiKoll.Application.Documents;

namespace KemiKoll.Application.Tests.Documents;

public class SafetyDataSheetTextParserTests
{
    // Shortened, realistic Swedish data sheet. Section 3 lists ingredient codes (H400, H410)
    // that are NOT the classification of the product and must be ignored.
    private const string SwedishSheet = """
        SÄKERHETSDATABLAD
        enligt förordning (EG) nr 1907/2006 (REACH)
        Revisionsdatum: 2025-03-10    Ersätter: 2022-01-15    Version: 4.0

        AVSNITT 1: Namnet på ämnet/blandningen och bolaget/företaget
        1.1 Produktbeteckning
        Handelsnamn: Bromsrengöring Extra
        1.3 Närmare upplysningar om den som tillhandahåller säkerhetsdatablad
        Leverantör
        Verkstadsprodukter AB
        Industrigatan 5, 553 03 Jönköping

        AVSNITT 2: Farliga egenskaper
        2.1 Klassificering av ämnet eller blandningen
        Flam. Liq. 2; H225   Asp. Tox. 1; H304   Skin Irrit. 2; H315   STOT SE 3; H336   Aquatic Chronic 2; H411
        2.2 Märkningsuppgifter
        Faropiktogram: GHS02, GHS07, GHS08, GHS09
        Faroangivelser
        H225 Mycket brandfarlig vätska och ånga.
        H304 Kan vara dödligt om det sväljs och kommer ner i luftvägarna.
        Skyddsangivelser
        P210 Får inte utsättas för värme.
        EUH066 Upprepad exponering kan ge torr eller sprucken hud.

        AVSNITT 3: Sammansättning/information om beståndsdelar
        n-heptan  CAS 142-82-5  H225, H304, H315, H336, H400, H410
        """;

    private const string EnglishSheet = """
        SAFETY DATA SHEET
        Date of issue: 01.06.2019
        Revision date: 15/09/2023

        SECTION 1. Identification of the substance/mixture and of the company/undertaking
        1.1 Product identifier
        Product name: Acetone Technical
        1.3 Details of the supplier of the safety data sheet
        Supplier: Example Chemicals Ltd, 1 Lab Road, London

        SECTION 2. Hazards identification
        Hazard pictograms: ghs02 GHS07
        Hazard statements: H225 Highly flammable liquid and vapour. H319, H336
        H300+H310+H330 Fatal if swallowed, in contact with skin or if inhaled.
        H360FD May damage fertility. May damage the unborn child.
        Supplemental: EUH066
        SECTION 3. Composition/information on ingredients
        Ingredient X H400 H410
        SECTION 16. Other information
        Full text of H-statements: H400 Very toxic to aquatic life.
        """;

    [Fact]
    public void Parse_SwedishSheet_FindsAllFacts()
    {
        var result = SafetyDataSheetTextParser.Parse(SwedishSheet);

        Assert.Equal("Bromsrengöring Extra", result.ProductName);
        Assert.Equal("Verkstadsprodukter AB", result.Supplier);
        Assert.Equal(new DateOnly(2025, 3, 10), result.RevisionDate);
        Assert.Equal("sv", result.LanguageCode);
        string[] expectedHazards = ["H225", "H304", "H315", "H336", "H411", "EUH066"];
        Assert.Equal(expectedHazards, result.HazardCodes);
        string[] expectedPictograms = ["GHS02", "GHS07", "GHS08", "GHS09"];
        Assert.Equal(expectedPictograms, result.PictogramCodes);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Parse_EnglishSheet_FindsAllFacts()
    {
        var result = SafetyDataSheetTextParser.Parse(EnglishSheet);

        Assert.Equal("Acetone Technical", result.ProductName);
        Assert.Equal("Example Chemicals Ltd", result.Supplier);
        Assert.Equal(new DateOnly(2023, 9, 15), result.RevisionDate);
        Assert.Equal("en", result.LanguageCode);
        string[] expectedPictograms = ["GHS02", "GHS07"];
        Assert.Equal(expectedPictograms, result.PictogramCodes);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Parse_IgnoresIngredientCodesInSection3And16()
    {
        var swedish = SafetyDataSheetTextParser.Parse(SwedishSheet);
        var english = SafetyDataSheetTextParser.Parse(EnglishSheet);

        Assert.DoesNotContain("H400", swedish.HazardCodes);
        Assert.DoesNotContain("H410", swedish.HazardCodes);
        Assert.DoesNotContain("H400", english.HazardCodes);
    }

    [Fact]
    public void Parse_SplitsCombinedStatementsAndKeepsSuffixCase()
    {
        var result = SafetyDataSheetTextParser.Parse(EnglishSheet);

        string[] expected = ["H225", "H319", "H336", "H300", "H310", "H330", "H360FD", "EUH066"];
        Assert.Equal(expected, result.HazardCodes);
    }

    [Theory]
    [InlineData("H360Fd", "H360Fd")]
    [InlineData("H361d", "H361d")]
    [InlineData("H350i", "H350i")]
    [InlineData("EUH201A", "EUH201A")]
    public void Parse_FindsSpecialCodes(string code, string expected)
    {
        var result = SafetyDataSheetTextParser.Parse($"SECTION 2: Hazards identification\n{code} text\nSECTION 3: Composition");

        Assert.Equal(expected, Assert.Single(result.HazardCodes));
    }

    [Fact]
    public void Parse_IgnoresPrecautionaryStatementsAndLookalikes()
    {
        var result = SafetyDataSheetTextParser.Parse(
            "SECTION 2: Hazards identification\nP210 P280 H1234 XH225 H500 CH3OH\nSECTION 3: Composition");

        Assert.Empty(result.HazardCodes);
    }

    [Fact]
    public void Parse_PrefersRevisionDateOverIssueDate()
    {
        Assert.Equal(new DateOnly(2023, 9, 15), SafetyDataSheetTextParser.Parse(EnglishSheet).RevisionDate);
    }

    [Theory]
    [InlineData("Revisionsdatum: 2024-11-05", 2024, 11, 5)]
    [InlineData("Revision date: 05.11.2024", 2024, 11, 5)]
    [InlineData("Datum för revidering 5/11/2024", 2024, 11, 5)]
    [InlineData("Utgivningsdatum: 2020-02-01", 2020, 2, 1)]
    [InlineData("Revisionsdatum:\n2024-11-05", 2024, 11, 5)]
    public void Parse_FindsDateFormats(string line, int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), SafetyDataSheetTextParser.Parse(line).RevisionDate);
    }

    [Fact]
    public void Parse_NoDate_WarnsUser()
    {
        var result = SafetyDataSheetTextParser.Parse("AVSNITT 2: Farliga egenskaper\nH225\nAVSNITT 3: Sammansättning");

        Assert.Null(result.RevisionDate);
        Assert.Contains(result.Warnings, w => w.Contains("revision date", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_NoSection2_UsesWholeTextAndWarns()
    {
        var result = SafetyDataSheetTextParser.Parse("Revisionsdatum: 2024-01-01\nH225 H319");

        string[] expected = ["H225", "H319"];
        Assert.Equal(expected, result.HazardCodes);
        Assert.Contains(result.Warnings, w => w.Contains("Section 2", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_Subsection21_IsNotTakenAsSection2Heading()
    {
        // "2.1" alone must not start section 2; the real heading comes later.
        var result = SafetyDataSheetTextParser.Parse(
            "2.1 Something H315\nSECTION 2: Hazards identification\nH225\nSECTION 3: Composition");

        Assert.Equal("H225", Assert.Single(result.HazardCodes));
    }

    [Fact]
    public void Parse_WindowsLineBreaks_GiveSameResult()
    {
        var result = SafetyDataSheetTextParser.Parse(SwedishSheet.Replace("\n", "\r\n", StringComparison.Ordinal));

        Assert.Equal("Bromsrengöring Extra", result.ProductName);
        Assert.Equal("Verkstadsprodukter AB", result.Supplier);
        Assert.Equal(6, result.HazardCodes.Count);
    }

    [Fact]
    public void Parse_UnknownLanguage_ReturnsNull()
    {
        Assert.Null(SafetyDataSheetTextParser.Parse("H225").LanguageCode);
    }

    [Fact]
    public void Parse_SupplierNameWithCommaButNoAddress_IsKept()
    {
        var result = SafetyDataSheetTextParser.Parse("Supplier: Smith, Jones & Co\nSECTION 2: Hazards identification");

        Assert.Equal("Smith, Jones & Co", result.Supplier);
    }
}
