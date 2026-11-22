using KemiKoll.Application.SafetyDataSheets;
using KemiKoll.Domain.Hazards;

namespace KemiKoll.Application.Products;

/// <summary>
/// The editable details of a product, as text for a form.
/// </summary>
/// <param name="Id">The product id.</param>
/// <param name="Name">The trade name.</param>
/// <param name="Supplier">The supplier.</param>
/// <param name="HazardCodes">Hazard statement codes, for example "H225".</param>
/// <param name="PictogramCodes">GHS pictogram codes, for example "GHS02".</param>
public sealed record ProductDetails(
    Guid Id,
    string Name,
    string Supplier,
    IReadOnlyList<string> HazardCodes,
    IReadOnlyList<string> PictogramCodes);

/// <summary>
/// The new details of an existing product, as entered by the user.
/// </summary>
/// <param name="Id">The product id.</param>
/// <param name="Name">The trade name.</param>
/// <param name="Supplier">The supplier.</param>
/// <param name="HazardCodes">Hazard statement codes, for example "H225".</param>
/// <param name="PictogramCodes">GHS pictogram codes, for example "GHS02".</param>
public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Supplier,
    IReadOnlyList<string> HazardCodes,
    IReadOnlyList<string> PictogramCodes);

/// <summary>The outcome of an update: saved, not found, or error messages the user can fix.</summary>
public sealed record UpdateProductResult
{
    private UpdateProductResult(bool succeeded, bool notFound, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        NotFound = notFound;
        Errors = errors;
    }

    /// <summary><see langword="true"/> if the changes were saved.</summary>
    public bool Succeeded { get; }

    /// <summary><see langword="true"/> if the product does not exist (any more).</summary>
    public bool NotFound { get; }

    /// <summary>Messages explaining why the changes were not saved. Empty on success.</summary>
    public IReadOnlyList<string> Errors { get; }

    internal static UpdateProductResult Success() => new(true, false, []);

    internal static UpdateProductResult Missing() => new(false, true, ["The product no longer exists."]);

    internal static UpdateProductResult Failure(IReadOnlyList<string> errors) => new(false, false, errors);
}

/// <summary>
/// Use cases for changing an existing product: load it for editing, save changes, delete it.
/// </summary>
/// <param name="products">Where products are stored.</param>
/// <param name="sheets">Where safety data sheets are stored; deleted together with their product.</param>
public sealed class EditProduct(IProductRepository products, ISafetyDataSheetRepository sheets)
{
    /// <summary>Loads a product for the edit form.</summary>
    /// <param name="id">The product id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The details, or <see langword="null"/> if the product does not exist.</returns>
    public async Task<ProductDetails?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await products.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return product is null
            ? null
            : new ProductDetails(
                product.Id,
                product.Name,
                product.Supplier,
                product.HazardStatements.Select(statement => statement.Code).ToList(),
                product.Pictograms.Select(pictogram => pictogram.ToCode()).ToList());
    }

    /// <summary>Validates the new details and saves them. Nothing changes if any value is invalid.</summary>
    /// <param name="command">The new details.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>Success, not found, or the validation errors.</returns>
    public async Task<UpdateProductResult> UpdateAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await products.GetAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return UpdateProductResult.Missing();
        }

        var errors = new List<string>();
        var hazardStatements = ProductInput.ParseHazardStatements(command.HazardCodes, errors);
        var pictograms = ProductInput.ParsePictograms(command.PictogramCodes, errors);
        if (errors.Count > 0)
        {
            return UpdateProductResult.Failure(errors);
        }

        try
        {
            product.Update(command.Name, command.Supplier, hazardStatements, pictograms);
        }
        catch (ArgumentException exception)
        {
            return UpdateProductResult.Failure([UserMessages.From(exception)]);
        }

        var saved = await products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        return saved ? UpdateProductResult.Success() : UpdateProductResult.Missing();
    }

    /// <summary>Deletes a product together with its safety data sheets.</summary>
    /// <param name="id">The product id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns><see langword="true"/> if deleted; <see langword="false"/> if it did not exist.</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // The database would also delete the data sheets (cascade), but the in-memory storage
        // would not; deleting them explicitly gives the same result with every storage.
        await sheets.DeleteForProductAsync(id, cancellationToken).ConfigureAwait(false);
        return await products.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
    }
}
