using System.Text.RegularExpressions;

namespace KemiKoll.Domain.SafetyDataSheets;

/// <summary>
/// A safety data sheet (säkerhetsdatablad) for one chemical product:
/// the uploaded PDF file and the facts needed to know whether it is still current.
/// </summary>
/// <remarks>
/// The product is referenced by its id (<see cref="ProductId"/>) instead of holding the
/// <c>ChemicalProduct</c> object itself. This keeps the two entities independent:
/// a new revision of a data sheet can be stored without changing the product.
/// </remarks>
public sealed partial class SafetyDataSheet
{
    /// <summary>The maximum length of <see cref="FileName"/>.</summary>
    public const int MaxFileNameLength = 255;

    /// <summary>The language code for Swedish (ISO 639-1).</summary>
    public const string SwedishLanguageCode = "sv";

    private SafetyDataSheet(Guid id, Guid productId, DateOnly revisionDate, string languageCode, string fileName)
    {
        Id = id;
        ProductId = productId;
        RevisionDate = revisionDate;
        LanguageCode = languageCode;
        FileName = fileName;
    }

    /// <summary>The unique identity of the data sheet.</summary>
    public Guid Id { get; }

    /// <summary>The id of the chemical product this data sheet describes.</summary>
    public Guid ProductId { get; }

    /// <summary>The revision date printed on the data sheet by the supplier.</summary>
    public DateOnly RevisionDate { get; }

    /// <summary>The language of the data sheet as a two-letter ISO 639-1 code, for example <c>sv</c> or <c>en</c>.</summary>
    public string LanguageCode { get; }

    /// <summary>The original file name of the uploaded PDF, for example <c>degreaser-x-sds.pdf</c>.</summary>
    public string FileName { get; }

    /// <summary>
    /// <see langword="true"/> if the data sheet is in Swedish.
    /// REACH requires the supplier to provide the data sheet in the official language
    /// of the country where the product is placed on the market, so a data sheet used
    /// in Sweden should be in Swedish.
    /// </summary>
    public bool IsInSwedish => LanguageCode == SwedishLanguageCode;

    /// <summary>
    /// Creates a safety data sheet record.
    /// </summary>
    /// <param name="productId">The id of the product the data sheet belongs to.</param>
    /// <param name="revisionDate">The revision date printed on the data sheet.</param>
    /// <param name="languageCode">Two-letter ISO 639-1 language code. Case and surrounding spaces are ignored.</param>
    /// <param name="fileName">The original PDF file name. Must end with <c>.pdf</c>.</param>
    /// <returns>The new data sheet with a new <see cref="Id"/>.</returns>
    /// <exception cref="ArgumentException">An argument is empty, the wrong format, or too long.</exception>
    public static SafetyDataSheet Create(Guid productId, DateOnly revisionDate, string languageCode, string fileName)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("A product id is required.", nameof(productId));
        }

        if (revisionDate == default)
        {
            throw new ArgumentException("A revision date is required.", nameof(revisionDate));
        }

        return new SafetyDataSheet(
            Guid.CreateVersion7(),
            productId,
            revisionDate,
            ValidateLanguageCode(languageCode),
            ValidateFileName(fileName));
    }

    /// <summary>
    /// The date from which the data sheet is due for review under the given policy.
    /// </summary>
    /// <param name="policy">The company's review policy.</param>
    /// <returns><see cref="RevisionDate"/> plus the policy's maximum age.</returns>
    public DateOnly ReviewDueDate(ReviewPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return RevisionDate.AddYears(policy.MaxAgeInYears);
    }

    /// <summary>
    /// Whether the data sheet should be checked with the supplier for a newer version.
    /// </summary>
    /// <param name="today">The current date. Passed in (not read from the clock) so the rule is easy to test.</param>
    /// <param name="policy">The company's review policy.</param>
    /// <returns><see langword="true"/> on or after <see cref="ReviewDueDate"/>.</returns>
    public bool IsDueForReview(DateOnly today, ReviewPolicy policy) => today >= ReviewDueDate(policy);

    private static string ValidateLanguageCode(string languageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);

        var normalized = languageCode.Trim().ToLowerInvariant();
        if (!LanguageCodePattern().IsMatch(normalized))
        {
            throw new ArgumentException(
                $"'{languageCode}' is not a two-letter language code (expected for example 'sv' or 'en').",
                nameof(languageCode));
        }

        return normalized;
    }

    private static string ValidateFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var trimmed = fileName.Trim();
        if (trimmed.Length > MaxFileNameLength)
        {
            throw new ArgumentException($"Must be at most {MaxFileNameLength} characters.", nameof(fileName));
        }

        if (!trimmed.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A safety data sheet must be a PDF file.", nameof(fileName));
        }

        return trimmed;
    }

    [GeneratedRegex("^[a-z]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex LanguageCodePattern();
}
