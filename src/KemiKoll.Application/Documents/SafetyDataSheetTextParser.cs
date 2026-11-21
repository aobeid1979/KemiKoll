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

        // A real safety data sheet has 16 numbered sections. A technical data sheet or a
        // product sheet does not, and must not be used in its place.
        var looksLikeSafetyDataSheet = CountSectionHeadings(text) >= MinSectionHeadings;
        if (!looksLikeSafetyDataSheet)
        {
            warnings.Add("This does not look like a safety data sheet: its numbered sections (1–16) were not found. A technical data sheet or product sheet is not enough; ask the supplier for the safety data sheet.");
        }

        var (section1, section2, section2Found) = SplitSections(text, warnings, looksLikeSafetyDataSheet);
        var hazardCodes = FindHazardCodes(section2);

        if (section2Found && hazardCodes.Count == 0)
        {
            warnings.Add("No H-codes were found in section 2. Some data sheets, for example from outside the EU, write only the hazard text. If the product is classified as hazardous, enter the codes by hand.");
        }

        var revisionDate = FindRevisionDate(text);
        if (revisionDate is null)
        {
            warnings.Add("No revision date was found. Enter it from the data sheet.");
        }

        return new SafetyDataSheetExtraction(
            ProductName: FindProductName(section1),
            Supplier: WithoutAddress(FindSupplier(section1)),
            RevisionDate: revisionDate,
            LanguageCode: DetectLanguage(text),
            HazardCodes: hazardCodes,
            PictogramCodes: FindPictogramCodes(section2),
            Warnings: warnings);
    }

    // Fewer numbered section headings than this means the document is not a safety data sheet.
    private const int MinSectionHeadings = 3;

    private static int CountSectionHeadings(string text) =>
        SectionHeadingNumber().Matches(text)
            .Select(match => int.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture))
            .Where(number => number is >= 1 and <= 16)
            .Distinct()
            .Count();

    // The specific labels first ("Product name: X"); the general heading
    // ("Product identifier" followed by the name) only when they are missing.
    private static string? FindProductName(string section1) =>
        FindLabelValue(section1, ProductNameLabels()) ?? FindLabelValue(section1, ProductIdentifierLabels());

    // Returns (text before section 2, text of section 2, whether section 2 was found).
    // If section 2 cannot be found, the whole text is used for both.
    private static (string Section1, string Section2, bool Section2Found) SplitSections(
        string text,
        List<string> warnings,
        bool looksLikeSafetyDataSheet)
    {
        var section2 = Section2Heading().Match(text);
        if (!section2.Success)
        {
            // Not repeated for documents already flagged as "not a safety data sheet".
            if (looksLikeSafetyDataSheet)
            {
                warnings.Add("Section 2 (hazards identification) was not found. The hazard codes were taken from the whole document and may include ingredients; check them carefully.");
            }

            return (text, text, false);
        }

        var section3 = Section3Heading().Match(text, section2.Index + section2.Length);
        var section2End = section3.Success ? section3.Index : text.Length;
        if (!section3.Success)
        {
            warnings.Add("Section 3 was not found, so the end of section 2 is uncertain. Check the hazard codes.");
        }

        return (text[..section2.Index], text[section2.Index..section2End], true);
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
            if (FindFirstDate(rest) is { } date)
            {
                return date;
            }
        }

        return null;
    }

    // Numeric dates ("2025-03-10", "10.03.2025") and dates with month names
    // ("25 January 2021", "4 October, 2012", "January 25, 2021", "25 januari 2021").
    // Returns the date that appears first.
    private static DateOnly? FindFirstDate(string text)
    {
        var candidates = new List<(int Index, DateOnly Date)>();

        var numeric = DateValue().Match(text);
        if (numeric.Success && TryParseDate(numeric.Value, out var numericDate))
        {
            candidates.Add((numeric.Index, numericDate));
        }

        // Several words can look like "<number> <word> <year>"; the first with a real month name counts.
        foreach (Match named in NamedMonthDate().Matches(text))
        {
            if (TryParseNamedDate(named, out var namedDate))
            {
                candidates.Add((named.Index, namedDate));
                break;
            }
        }

        return candidates.Count == 0 ? null : candidates.MinBy(candidate => candidate.Index).Date;
    }

    private static bool TryParseNamedDate(Match match, out DateOnly date)
    {
        date = default;
        var day = match.Groups["day1"].Success ? match.Groups["day1"].Value : match.Groups["day2"].Value;
        var monthName = match.Groups["month1"].Success ? match.Groups["month1"].Value : match.Groups["month2"].Value;

        if (MonthNumber(monthName) is not { } month
            || !int.TryParse(day, CultureInfo.InvariantCulture, out var dayNumber)
            || !int.TryParse(match.Groups["year"].Value, CultureInfo.InvariantCulture, out var year)
            || year < 1900 || year > 2100
            || dayNumber < 1 || dayNumber > DateTime.DaysInMonth(year, month))
        {
            return false;
        }

        date = new DateOnly(year, month, dayNumber);
        return true;
    }

    // English and Swedish month names; the first three letters are enough ("Jan", "okt.").
    private static readonly string[][] MonthNames =
    [
        ["jan"], ["feb"], ["mar"], ["apr"], ["may", "maj"], ["jun"],
        ["jul"], ["aug"], ["sep"], ["oct", "okt"], ["nov"], ["dec"],
    ];

    private static int? MonthNumber(string name)
    {
        if (name.Length < 3)
        {
            return null;
        }

        var start = name[..3];
        for (var i = 0; i < MonthNames.Length; i++)
        {
            if (MonthNames[i].Any(prefix => string.Equals(prefix, start, StringComparison.OrdinalIgnoreCase)))
            {
                return i + 1;
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

        // In table-like layouts the next column follows on the same line after a wide gap
        // ("Product Identifier    Solvent        Version: 8"); keep only the first column.
        var value = CleanValue(match.Groups["value"].Value);
        if (value.Length == 0)
        {
            var nextLine = NextNonEmptyLine().Match(section, match.Index + match.Length);
            value = nextLine.Success ? CleanValue(nextLine.Groups["line"].Value) : string.Empty;
        }
        if (value.Length == 0)
        {
            return null;
        }

        return value.Length > MaxTextValueLength ? value[..MaxTextValueLength] : value;
    }

    // The supplier label is sometimes followed by other lines first (another field's value in a
    // two-column table, or an address). Among the first few lines after the label, prefer one that
    // looks like a company name ("… AB", "… Ltd.", "… GmbH"); otherwise take the first.
    private static string? FindSupplier(string section1)
    {
        // A "Supplier:" label first; otherwise the heading of section 1.3, which in an EU data
        // sheet always holds the supplier details, often with the company name on the next line.
        var match = SupplierLabels().Match(section1);
        if (!match.Success)
        {
            match = SupplierSectionHeading().Match(section1);
        }

        if (!match.Success)
        {
            return null;
        }

        var candidates = new List<string>();
        // The section heading has no "value" group, so its same-line value is empty.
        var sameLine = CleanValue(match.Groups["value"].Value);
        if (sameLine.Length > 0)
        {
            candidates.Add(sameLine);
        }

        var position = match.Index + match.Length;
        while (candidates.Count < SupplierCandidateLines)
        {
            var nextLine = NextNonEmptyLine().Match(section1, position);
            if (!nextLine.Success)
            {
                break;
            }

            candidates.Add(CleanValue(nextLine.Groups["line"].Value));
            position = nextLine.Index + nextLine.Length;
        }

        var supplier = candidates.FirstOrDefault(candidate => CompanySuffix().IsMatch(candidate))
            ?? candidates.FirstOrDefault(candidate => candidate.Length > 0);

        return supplier is null or "" ? null : Truncate(supplier);
    }

    private const int SupplierCandidateLines = 4;

    private static string CleanValue(string value)
    {
        var firstColumn = ColumnGap().Split(value.Trim())[0];

        // A second field on the same line: "Solvent Version: 8" → "Solvent".
        return NextFieldOnSameLine().Replace(firstColumn, string.Empty).Trim(' ', ':', '-', '–', '\t');
    }

    private static string Truncate(string value) =>
        value.Length > MaxTextValueLength ? value[..MaxTextValueLength] : value;

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
        @"Revisionsdatum|Datum\s+för\s+revidering|Ändringsdatum|Senast\s+(?:ändrad|reviderad)|Omarbetad|Reviderad|Revision\s*date|Date\s+of\s+(?:the\s+)?(?:latest\s+|last\s+)?revision|Last\s+revised|Revised(?:\s+on)?|Effective\s+date",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RevisionDateLabels();

    [GeneratedRegex(
        @"Utfärdandedatum|Utgivningsdatum|Upprättad|Utarbetad|Datum\s+för\s+utfärdande|Issue\s*date|Date\s+of\s+issue|Date\s+of\s+compilation|Date\s+prepared|Prepared\s+on",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IssueDateLabels();

    [GeneratedRegex(@"\d{4}-\d{1,2}-\d{1,2}|\d{1,2}[./-]\d{1,2}[./-]\d{4}|\d{4}\.\d{2}\.\d{2}", RegexOptions.CultureInvariant)]
    private static partial Regex DateValue();

    // "25 January 2021", "4 October, 2012", "25 jan. 2021", "25 januari 2021" or "January 25, 2021".
    [GeneratedRegex(
        @"(?:(?<day1>\d{1,2})\.?\s+(?<month1>[A-Za-zÅÄÖåäö]{3,9})\.?,?|(?<month2>[A-Za-zÅÄÖåäö]{3,9})\.?\s+(?<day2>\d{1,2}),?)\s+(?<year>\d{4})",
        RegexOptions.CultureInvariant)]
    private static partial Regex NamedMonthDate();

    // A tab or three or more spaces: the gap between two columns of a table-like layout.
    [GeneratedRegex(@"\t|[ ]{3,}", RegexOptions.CultureInvariant)]
    private static partial Regex ColumnGap();

    // A known field label after the value on the same line (" Version: 8", " Revision date: …").
    // Removed together with everything after it. A fixed list, so product names stay whole.
    [GeneratedRegex(
        @"\s+(?:Version(?:snummer)?|Revision(?:\s*date)?|Revisionsdatum|Utgåva|Ersätter|Supersedes|Effective\s+date|Issue\s+date|Date|Datum|Page|Sida)\s*:.*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NextFieldOnSameLine();

    // Numbered section headings: "AVSNITT 3:", "SECTION 3.", "Section 12. Ecological information".
    [GeneratedRegex(
        @"^[ \t]*(?:AVSNITT|SECTION)[ \t]*(?<number>\d{1,2})(?!\.?\d)",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex SectionHeadingNumber();

    [GeneratedRegex(
        @"^[ \t]*(?:1\.1[ \t]*)?(?:Handelsnamn|Produktnamn|Trade[ \t]+name|Product[ \t]+name)(?![\p{L}'’])[ \t]*[:\-–]?[ \t]*(?<value>[^\r\n]*)\r?$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex ProductNameLabels();

    // General heading of section 1.1; the name follows on the same line or the next one.
    [GeneratedRegex(
        @"^[ \t]*(?:1\.1[ \t]*)?(?:Product[ \t]+identifier|Produktbeteckning|Produktidentifierare)(?![\p{L}'’])[ \t]*[:\-–]?[ \t]*(?<value>[^\r\n]*)\r?$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex ProductIdentifierLabels();

    // "Initial Supplier Identifier" (North American sheets) may be split over two lines.
    // The label must end at a word boundary, so "supplier's" in running text is not a label.
    [GeneratedRegex(
        @"^[ \t]*(?:(?:Initial\s+)?Supplier(?:\s+identifier)?|Leverantör|Tillverkare|Manufacturer|Företag|Company(?:[ \t]+name)?)(?![\p{L}'’])[ \t]*[:\-–]?[ \t]*(?<value>[^\r\n]*)\r?$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex SupplierLabels();

    // "1.3 Närmare upplysningar om den som tillhandahåller säkerhetsdatablad",
    // "1.3 Details of the supplier of the safety data sheet". Not "1.30".
    [GeneratedRegex(@"^[ \t]*1\.3(?!\d)[^\r\n]*\r?$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex SupplierSectionHeading();

    [GeneratedRegex(@"\G\s*?\r?\n[ \t]*(?<line>[^\r\n]*\S[^\r\n]*)", RegexOptions.CultureInvariant)]
    private static partial Regex NextNonEmptyLine();

    // Legal-form endings of company names in Sweden, the Nordics, the EU, the UK and North America.
    [GeneratedRegex(
        @"(?<![\p{L}])(?:AB|HB|KB|Oy|Oyj|AS|ASA|A/S|ApS|GmbH|AG|KG|S\.?A\.?|S\.?r\.?l\.?|S\.?p\.?A\.?|B\.?V\.?|N\.?V\.?|Ltd\.?|Limited|plc|Inc\.?|LLC|Corp\.?|Corporation|Co\.)(?![\p{L}])",
        RegexOptions.CultureInvariant)]
    private static partial Regex CompanySuffix();

    [GeneratedRegex(@"\b(?:AVSNITT|Säkerhetsdatablad|Farliga\s+egenskaper|Leverantör|Faroangivelser)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SwedishWords();

    [GeneratedRegex(@"\b(?:SECTION|Safety\s+data\s+sheet|Hazards?\s+identification|Supplier|Hazard\s+statements)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EnglishWords();
}
