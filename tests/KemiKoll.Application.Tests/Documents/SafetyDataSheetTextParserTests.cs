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
        var result = SafetyDataSheetTextParser.Parse(
            "SECTION 1: Identification\nRevision date: 2024-01-01\nH225 H319\nSECTION 4: First aid\nSECTION 5: Fire");

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

    // ----- Found with real data sheets (structure only; names and text are fictional) -----

    // North American (WHMIS/OSHA) layout as extracted from a two-column table:
    // all labels first, then all values. Hazard statements are written as text, without codes.
    private const string NorthAmericanSheetLabelsFirst = """
        Safety Data Sheet
        Section 1. Identification
        Version: 8
        Effective Date: 25 January 2021

        Product Identifier

        Parts Cleaner

        Other Means Of
        Identification
        Initial Supplier
        Identifier

        Mineral spirits
        Example Solvents Ltd.
        100 – 1 Street SE
        Section 2. Hazard Identification
        Signal Word Danger
        Hazard Statement Flammable liquid and vapour. Causes skin irritation.
        Section 3. Composition / Information on Ingredients
        Section 16. Other Information
        Date Prepared: 4 October, 2012
        """;

    // The same sheet when the text keeps the table rows: label and value on one line.
    private const string NorthAmericanSheetRows = """
        Safety Data Sheet
        Section 1. Identification
        Product Identifier    Parts Cleaner                       Version: 8
                                                                  Effective Date: 25 January 2021
        Initial Supplier        Example Solvents Ltd.
        Identifier              100 – 1 Street SE
        Section 2. Hazard Identification
        Hazard Statement        Flammable liquid and vapour.
        Section 3. Composition / Information on Ingredients
        """;

    [Theory]
    [InlineData(NorthAmericanSheetLabelsFirst)]
    [InlineData(NorthAmericanSheetRows)]
    public void Parse_NorthAmericanSheet_FindsNameSupplierAndDate(string text)
    {
        var result = SafetyDataSheetTextParser.Parse(text);

        Assert.Equal("Parts Cleaner", result.ProductName);
        Assert.Equal("Example Solvents Ltd.", result.Supplier);
        Assert.Equal(new DateOnly(2021, 1, 25), result.RevisionDate);
        Assert.Equal("en", result.LanguageCode);
    }

    [Fact]
    public void Parse_HazardTextWithoutCodes_WarnsToEnterCodes()
    {
        var result = SafetyDataSheetTextParser.Parse(NorthAmericanSheetLabelsFirst);

        Assert.Empty(result.HazardCodes);
        Assert.Contains(result.Warnings, w => w.Contains("No H-codes were found in section 2", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_TechnicalDataSheet_IsFlaggedAsNotASafetyDataSheet()
    {
        // A technical (product) data sheet has properties and specifications, but not the 16 sections.
        const string technicalDataSheet = """
            METHANOL TECHNICAL DATA SHEET
            CAS No: 67-56-1
            TYPICAL PROPERTIES: Boiling point 64.6 °C, flash point 12 °C
            HAZARDS & PRECAUTIONS: Flammable. Toxic if swallowed. Refer to the supplier's Safety Data Sheet.
            """;

        var result = SafetyDataSheetTextParser.Parse(technicalDataSheet);

        Assert.Contains(result.Warnings, w => w.StartsWith("This does not look like a safety data sheet", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Warnings, w => w.StartsWith("Section 2", StringComparison.Ordinal));
        Assert.Null(result.Supplier); // "supplier's" in running text is not a label
    }

    [Theory]
    [InlineData("Revision date: 25 January 2021", 2021, 1, 25)]
    [InlineData("Date of latest revision: 4 October, 2012", 2012, 10, 4)]
    [InlineData("Revised: January 25, 2021", 2021, 1, 25)]
    [InlineData("Revisionsdatum: 25 januari 2021", 2021, 1, 25)]
    [InlineData("Revisionsdatum: 3 okt. 2024", 2024, 10, 3)]
    [InlineData("Senast reviderad 12 maj 2023", 2023, 5, 12)]
    public void Parse_FindsDatesWithMonthNames(string line, int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), SafetyDataSheetTextParser.Parse(line).RevisionDate);
    }

    [Theory]
    [InlineData("Revision date: 31 February 2021")]
    [InlineData("Revision date: page 2 of 2021")]
    public void Parse_InvalidNamedDate_IsIgnored(string line)
    {
        Assert.Null(SafetyDataSheetTextParser.Parse(line).RevisionDate);
    }

    [Fact]
    public void Parse_EffectiveDate_PreferredOverDatePrepared()
    {
        Assert.Equal(new DateOnly(2021, 1, 25), SafetyDataSheetTextParser.Parse(NorthAmericanSheetLabelsFirst).RevisionDate);
    }

    [Fact]
    public void Parse_ProductNameLabel_PreferredOverProductIdentifierHeading()
    {
        var result = SafetyDataSheetTextParser.Parse("1.1 Product identifier\nProduct name: Degreaser X\nSECTION 2: Hazards identification");

        Assert.Equal("Degreaser X", result.ProductName);
    }
}
