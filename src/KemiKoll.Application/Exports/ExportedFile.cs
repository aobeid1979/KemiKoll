namespace KemiKoll.Application.Exports;

/// <summary>A file ready to download.</summary>
/// <param name="FileName">The suggested file name, for example "kemikoll-inventory-2026-11-23.xlsx".</param>
/// <param name="ContentType">The MIME type of the file.</param>
/// <param name="Content">The file contents.</param>
public sealed record ExportedFile(string FileName, string ContentType, byte[] Content);
