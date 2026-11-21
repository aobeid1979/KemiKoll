using KemiKoll.Application.SafetyDataSheets;
using KemiKoll.Domain.Hazards;
using KemiKoll.Domain.Products;
using KemiKoll.Domain.SafetyDataSheets;

namespace KemiKoll.Application.Products;

/// <summary>
/// The data needed to add a product, as entered by the user.
/// Codes are plain text here; the use case parses and validates them.
/// </summary>
/// <param name="Name">The trade name.</param>
/// <param name="Supplier">The supplier.</param>
/// <param name="HazardCodes">Hazard statement codes, for example "H225".</param>
/// <param name="PictogramCodes">GHS pictogram codes, for example "GHS02".</param>
/// <param name="DataSheet">
/// The safety data sheet the details came from, or <see langword="null"/> when the product is entered by hand.
/// </param>
public sealed record AddProductCommand(
    string Name,
    string Supplier,
    IReadOnlyList<string> HazardCodes,
    IReadOnlyList<string> PictogramCodes,
    NewDataSheet? DataSheet = null);

/// <summary>
/// A safety data sheet to register together with a new product.
/// </summary>
/// <param name="RevisionDate">The revision date printed on the data sheet.</param>
/// <param name="LanguageCode">Two-letter language code, for example "sv".</param>
/// <param name="FileName">The original PDF file name.</param>
public sealed record NewDataSheet(DateOnly RevisionDate, string LanguageCode, string FileName);

/// <summary>
/// The outcome of adding a product: either the new product id, or a list of
/// error messages the user can fix.
/// </summary>
public sealed record AddProductResult
{
    private AddProductResult(Guid? productId, IReadOnlyList<string> errors)
    {
        ProductId = productId;
        Errors = errors;
    }

    /// <summary>The id of the new product, or <see langword="null"/> if it was not added.</summary>
    public Guid? ProductId { get; }

    /// <summary>Messages explaining why the product was not added. Empty on success.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary><see langword="true"/> if the product was added.</summary>
    public bool Succeeded => ProductId is not null;

    internal static AddProductResult Success(Guid productId) => new(productId, []);

    internal static AddProductResult Failure(IReadOnlyList<string> errors) => new(null, errors);
}

/// <summary>
/// Use case: add a chemical product to the inventory.
/// </summary>
/// <remarks>
/// Invalid user input (an unknown code, an empty name) is returned as error messages
/// instead of exceptions, so a form can show all problems at once.
/// </remarks>
/// <param name="repository">Where products are stored.</param>
/// <param name="sheets">Where safety data sheets are stored, when the command includes one.</param>
public sealed class AddProduct(IProductRepository repository, ISafetyDataSheetRepository sheets)
{
    /// <summary>
    /// Validates the input, creates the product and saves it, together with its data sheet if one is given.
    /// </summary>
    /// <param name="command">The data entered by the user.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The new product id, or the validation errors.</returns>
    public async Task<AddProductResult> ExecuteAsync(AddProductCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();
        var hazardStatements = ParseHazardStatements(command.HazardCodes, errors);
        var pictograms = ParsePictograms(command.PictogramCodes, errors);

        ChemicalProduct? product = null;
        try
        {
            product = ChemicalProduct.Create(command.Name, command.Supplier, hazardStatements);
        }
        catch (ArgumentException exception)
        {
            // The domain rules (required name, max length …) are the single source of truth;
            // their message is passed on to the user.
            errors.Add(UserMessages.From(exception));
        }

        // The data sheet is validated BEFORE anything is saved, so an invalid data sheet
        // never leaves a product behind without it.
        var sheet = product is null ? null : CreateDataSheet(product.Id, command.DataSheet, errors);

        if (product is null || errors.Count > 0)
        {
            return AddProductResult.Failure(errors);
        }

        foreach (var pictogram in pictograms)
        {
            product.AddPictogram(pictogram);
        }

        await repository.AddAsync(product, cancellationToken).ConfigureAwait(false);
        if (sheet is not null)
        {
            await sheets.AddAsync(sheet, cancellationToken).ConfigureAwait(false);
        }

        return AddProductResult.Success(product.Id);
    }

    private static SafetyDataSheet? CreateDataSheet(Guid productId, NewDataSheet? dataSheet, List<string> errors)
    {
        if (dataSheet is null)
        {
            return null;
        }

        try
        {
            return SafetyDataSheet.Create(productId, dataSheet.RevisionDate, dataSheet.LanguageCode, dataSheet.FileName);
        }
        catch (ArgumentException exception)
        {
            errors.Add("Data sheet – " + UserMessages.From(exception));
            return null;
        }
    }

    private static List<HazardStatement> ParseHazardStatements(IReadOnlyList<string>? codes, List<string> errors)
    {
        var statements = new List<HazardStatement>();
        foreach (var code in codes ?? [])
        {
            if (HazardStatement.TryParse(code, out var statement))
            {
                statements.Add(statement);
            }
            else
            {
                errors.Add($"'{code}' is not a valid hazard statement code (for example H225 or EUH066).");
            }
        }

        return statements;
    }

    private static List<Pictogram> ParsePictograms(IReadOnlyList<string>? codes, List<string> errors)
    {
        var pictograms = new List<Pictogram>();
        foreach (var code in codes ?? [])
        {
            if (Domain.Hazards.PictogramCodes.TryParse(code, out var pictogram))
            {
                pictograms.Add(pictogram);
            }
            else
            {
                errors.Add($"'{code}' is not a valid pictogram code (GHS01 to GHS09).");
            }
        }

        return pictograms;
    }
}
