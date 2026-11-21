namespace KemiKoll.Application.Documents;

/// <summary>
/// The facts found in the text of a safety data sheet. Every value is a <b>suggestion</b>
/// that the user checks and confirms before anything is saved.
/// </summary>
/// <param name="ProductName">Trade name from section 1, or <see langword="null"/> if not found.</param>
/// <param name="Supplier">Supplier from section 1, or <see langword="null"/> if not found.</param>
/// <param name="RevisionDate">Revision date (or issue date if there is no revision date), or <see langword="null"/>.</param>
/// <param name="LanguageCode">"sv" or "en" when the language is recognised, otherwise <see langword="null"/>.</param>
/// <param name="HazardCodes">H- and EUH-codes from section 2, in order of appearance, without duplicates.</param>
/// <param name="PictogramCodes">GHS codes (GHS01–GHS09) written in section 2, in official order.</param>
/// <param name="Warnings">Things the user should check, for example "section 2 was not found".</param>
public sealed record SafetyDataSheetExtraction(
    string? ProductName,
    string? Supplier,
    DateOnly? RevisionDate,
    string? LanguageCode,
    IReadOnlyList<string> HazardCodes,
    IReadOnlyList<string> PictogramCodes,
    IReadOnlyList<string> Warnings);
