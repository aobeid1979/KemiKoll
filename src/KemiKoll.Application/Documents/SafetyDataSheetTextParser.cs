using System.Globalization;
using System.Text.RegularExpressions;

using KemiKoll.Domain.Hazards;

namespace KemiKoll.Application.Documents;

/// <summary>
/// Finds the facts in the text of a safety data sheet with fixed rules (no AI):
/// hazard codes, pictogram codes, revision date, language, product name and supplier.
/// </summary>
/// <remarks>
/// <para>
/// Safety data sheets follow the 16-section structure of REACH Annex II, in Swedish
/// ("AVSNITT 2: Farliga egenskaper") or English ("SECTION 2: Hazards identification").
/// The rules rely on that structure.
/// </para>
/// <para>
/// Hazard and pictogram codes are taken from <b>section 2 only</b>. Section 3 lists the
/// codes of each ingredient and section 16 repeats their full text; those codes are not
/// the classification of the product itself and would give a wrong result.
/// </para>
/// <para>
/// Everything returned is a suggestion; the user confirms it in the form.
/// </para>
/// </remarks>
public static partial class SafetyDataSheetTextParser
{
    private const int MaxTextValueLength = 200;

    /// <summary>Extracts the facts from the full text of a safety data sheet.</summary>
    /// <param name="text">The text of all pages.</param>
    /// <returns>The suggestions and any warnings.</returns>
    public static SafetyDataSheetExtraction Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var warnings = new List<string>();
        var (section1, section2) = SplitSections(text, warnings);

        var revisionDate = FindRevisionDate(text);
        if (revisionDate is null)
        {
            warnings.Add("No revision date was found. Enter it from the data sheet.");
        }

        var pictogramCodes = FindPictogramCodes(section2);

        return new SafetyDataSheetExtraction(
            ProductName: FindLabelValue(section1, ProductNameLabels()),
            Supplier: WithoutAddress(FindLabelValue(section1, SupplierLabels())),
            RevisionDate: revisionDate,
            LanguageCode: DetectLanguage(text),
            HazardCodes: FindHazardCodes(section2),
            PictogramCodes: pictogramCodes,
            Warnings: warnings);
    }

    // Returns (text before section 2, text of section 2).
    // If section 2 cannot be found, the whole text is used for both, with a warning.
    private static (string Section1, string Section2) SplitSections(string text, List<string> warnings)
    {
        var section2 = Section2Heading().Match(text);
        if (!section2.Success)
        {
            warnings.Add("Section 2 (hazards identification) was not found. The hazard codes were taken from the whole document and may include ingredients; check them carefully.");
            return (text, text);
        }

        var section3 = Section3Heading().Match(text, section2.Index + section2.Length);
        var section2End = section3.Success ? section3.Index : text.Length;
        if (!section3.Success)
        {
            warnings.Add("Section 3 was not found, so the end of section 2 is uncertain. Check the hazard codes.");
        }

        return (text[..section2.Index], text[section2.Index..section2End]);
    }

    private static List<string> FindHazardCodes(string section)
    {
        var codes = new List<string>();

        // Combined statements such as "H300+H310+H330" are matched code by code.
        foreach (Match match in HazardCode().Matches(section))
        {
            if (HazardStatement.TryParse(match.Value, out var statement) && !codes.Contains(statement.Code))
            {
                codes.Add(statement.Code);
            }
        }

        return codes;
    }

    private static List<string> FindPictogramCodes(string section)
    {
        var found = new HashSet<Pictogram>();
        foreach (Match match in PictogramCode().Matches(section))
        {
            if (PictogramCodes.TryParse(match.Value, out var pictogram))
            {
                found.Add(pictogram);
            }
        }

        // Official order, the same as in the form.
        return Enum.GetValues<Pictogram>().Where(found.Contains).Select(p => p.ToCode()).ToList();
    }

    private static DateOnly? FindRevisionDate(string text) =>
        FindDateAfterLabel(text, RevisionDateLabels()) ?? FindDateAfterLabel(text, IssueDateLabels());

    private static DateOnly? FindDateAfterLabel(string text, Regex labels)
    {
        foreach (Match label in labels.Matches(text))
        {
            // The date follows the label on the same line or the next one.
            var rest = text.Substring(label.Index + label.Length, Math.Min(60, text.Length - label.Index - label.Length));
            var date = DateValue().Match(rest);
            if (date.Success && TryParseDate(date.Value, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static readonly string[] DateFormats =
        ["yyyy-MM-dd", "yyyy-M-d", "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy.MM.dd"];

    private static bool TryParseDate(string value, out DateOnly date) =>
        DateOnly.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static string? DetectLanguage(string text)
    {
        var swedish = SwedishWords().Count(text);
        var english = EnglishWords().Count(text);

        if (swedish == english)
        {
            return null;
        }

        return swedish > english ? "sv" : "en";
    }

    // Finds "Label: value" on one line, or "Label" with the value on the next non-empty line.
    private static string? FindLabelValue(string section, Regex labels)
    {
        var match = labels.Match(section);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups["value"].Value.Trim();
        if (value.Length == 0)
        {
            var nextLine = NextNonEmptyLine().Match(section, match.Index + match.Length);
            value = nextLine.Success ? nextLine.Groups["line"].Value.Trim() : string.Empty;
        }

        value = value.Trim(' ', ':', '-', '–', '\t');
        if (value.Length == 0)
        {
            return null;
        }

        return value.Length > MaxTextValueLength ? value[..MaxTextValueLength] : value;
    }

    // "Example Chemicals Ltd, 1 Lab Road, London" → "Example Chemicals Ltd".
    // Only cut when the rest contains a digit (street number, postcode), so names with commas survive.
    private static string? WithoutAddress(string? supplier)
    {
        var comma = supplier?.IndexOf(',', StringComparison.Ordinal) ?? -1;
        return comma > 0 && supplier![comma..].Any(char.IsDigit) ? supplier[..comma].TrimEnd() : supplier;
    }

    // --- Patterns ------------------------------------------------------------------------

    // "AVSNITT 2: Farliga egenskaper", "SECTION 2. Hazards identification", "2. Farliga egenskaper".
    // "2.1" (a subsection) and "21" are not headings.
    [GeneratedRegex(
        @"^[ \t]*(?:(?:AVSNITT|SECTION)[ \t]*2(?!\.?\d)|2\.?[ \t]+(?:Farliga[ \t]+egenskaper|Farlighetsidentifiering|Hazards?[ \t]+identification))",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex Section2Heading();

    [GeneratedRegex(
        @"^[ \t]*(?:(?:AVSNITT|SECTION)[ \t]*3(?!\.?\d)|3\.?[ \t]+(?:Sammansättning|Composition))",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex Section3Heading();

    // H200–H499 with the CLP suffixes (case matters: F/D = may damage, f/d = suspected;
    // longest first, so "H360FD" is not cut to "H360F"), or EUH + 3 digits.
    [GeneratedRegex(@"(?<![A-Za-z0-9])(?:EUH\d{3}A?|H[2-4]\d{2}(?:FD|Fd|Df|fd|F|D|f|d|i)?)(?![A-Za-z0-9])", RegexOptions.CultureInvariant)]
    private static partial Regex HazardCode();

    [GeneratedRegex(@"(?<![A-Za-z0-9])GHS0[1-9](?!\d)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PictogramCode();

    [GeneratedRegex(
        @"Revisionsdatum|Datum\s+för\s+revidering|Omarbetad|Reviderad|Revision\s*date|Date\s+of\s+revision|Revised(?:\s+on)?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RevisionDateLabels();

    [GeneratedRegex(
        @"Utfärdandedatum|Utgivningsdatum|Upprättad|Datum\s+för\s+utfärdande|Issue\s*date|Date\s+of\s+issue|Date\s+of\s+compilation",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IssueDateLabels();

    [GeneratedRegex(@"\d{4}-\d{1,2}-\d{1,2}|\d{1,2}[./-]\d{1,2}[./-]\d{4}|\d{4}\.\d{2}\.\d{2}", RegexOptions.CultureInvariant)]
    private static partial Regex DateValue();

    [GeneratedRegex(
        @"^[ \t]*(?:1\.1[ \t]*)?(?:Handelsnamn|Produktnamn|Trade[ \t]+name|Product[ \t]+name)[ \t]*[:\-–]?[ \t]*(?<value>[^\r\n]*)\r?$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex ProductNameLabels();

    [GeneratedRegex(
        @"^[ \t]*(?:Leverantör|Supplier|Tillverkare|Manufacturer|Företag|Company(?:[ \t]+name)?)[ \t]*[:\-–]?[ \t]*(?<value>[^\r\n]*)\r?$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex SupplierLabels();

    [GeneratedRegex(@"\G\s*?\r?\n[ \t]*(?<line>[^\r\n]*\S[^\r\n]*)", RegexOptions.CultureInvariant)]
    private static partial Regex NextNonEmptyLine();

    [GeneratedRegex(@"\b(?:AVSNITT|Säkerhetsdatablad|Farliga\s+egenskaper|Leverantör|Faroangivelser)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SwedishWords();

    [GeneratedRegex(@"\b(?:SECTION|Safety\s+data\s+sheet|Hazards?\s+identification|Supplier|Hazard\s+statements)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EnglishWords();
}
