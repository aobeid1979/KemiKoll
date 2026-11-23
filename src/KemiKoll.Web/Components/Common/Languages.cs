namespace KemiKoll.Web.Components.Common;

/// <summary>
/// The languages a safety data sheet can be registered in, for the language drop-downs.
/// </summary>
internal static class Languages
{
    /// <summary>Two-letter code and English name, Swedish first.</summary>
    public static readonly IReadOnlyList<(string Code, string Name)> All =
    [
        ("sv", "Swedish"),
        ("en", "English"),
        ("no", "Norwegian"),
        ("da", "Danish"),
        ("fi", "Finnish"),
        ("de", "German"),
    ];

    /// <summary>The English name of a language code, or the code itself if it is not in the list.</summary>
    /// <param name="languageCode">A two-letter code such as "sv".</param>
    /// <returns>For example "Swedish".</returns>
    public static string Name(string languageCode) =>
        All.FirstOrDefault(language => language.Code == languageCode).Name ?? languageCode;
}
