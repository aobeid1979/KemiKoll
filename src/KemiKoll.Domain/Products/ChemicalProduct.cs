using System.Diagnostics.CodeAnalysis;

using KemiKoll.Domain.Hazards;

namespace KemiKoll.Domain.Products;

/// <summary>
/// A chemical product used or stored at a workplace, for example a degreaser,
/// a cutting fluid or a hair dye. One row in the chemical inventory (kemikalieförteckning).
/// </summary>
/// <remarks>
/// This is an entity: it has an identity (<see cref="Id"/>) that stays the same
/// when its name, supplier or hazard statements change.
/// All changes go through methods, so the product is always in a valid state.
/// </remarks>
public sealed class ChemicalProduct
{
    /// <summary>The maximum length of <see cref="Name"/> and <see cref="Supplier"/>.</summary>
    public const int MaxTextLength = 200;

    // A list (not a set) keeps the order from the safety data sheet;
    // duplicates are prevented in AddHazardStatement.
    // Not readonly: EF Core assigns this field when it loads a product from the database.
    [SuppressMessage("Style", "IDE0044:Make field readonly", Justification = "Set by EF Core when loading from the database.")]
    private List<HazardStatement> _hazardStatements = [];

    // Same approach for pictograms: order kept, duplicates prevented in AddPictogram.
    // Not readonly for the same reason.
    [SuppressMessage("Style", "IDE0044:Make field readonly", Justification = "Set by EF Core when loading from the database.")]
    private List<Pictogram> _pictograms = [];

    private ChemicalProduct(Guid id, string name, string supplier, DateTimeOffset addedAt)
    {
        Id = id;
        Name = name;
        Supplier = supplier;
        AddedAt = addedAt;
        LastChangedAt = addedAt;
    }

    /// <summary>The unique identity of the product.</summary>
    public Guid Id { get; }

    /// <summary>The trade name as written on the safety data sheet, for example "Hycut ET 46".</summary>
    public string Name { get; private set; }

    /// <summary>The company that supplies the product and issues its safety data sheet.</summary>
    public string Supplier { get; private set; }

    /// <summary>When the product was added to the inventory (UTC).</summary>
    public DateTimeOffset AddedAt { get; private set; }

    /// <summary>
    /// When the product details were last changed with <see cref="Update"/> (UTC).
    /// Equal to <see cref="AddedAt"/> until the first real change.
    /// </summary>
    public DateTimeOffset LastChangedAt { get; private set; }

    /// <summary>The hazard statements (H-phrases) of the product, in the order they were added.</summary>
    public IReadOnlyList<HazardStatement> HazardStatements => _hazardStatements;

    /// <summary>The GHS hazard pictograms of the product, in the order they were added.</summary>
    public IReadOnlyList<Pictogram> Pictograms => _pictograms;

    /// <summary>
    /// <see langword="true"/> if the product has at least one hazard statement or pictogram.
    /// Hazardous products need a safety data sheet and a risk assessment.
    /// </summary>
    public bool IsHazardous => _hazardStatements.Count > 0 || _pictograms.Count > 0;

    /// <summary>
    /// Creates a new chemical product.
    /// </summary>
    /// <param name="name">The trade name. Surrounding spaces are removed.</param>
    /// <param name="supplier">The supplier. Surrounding spaces are removed.</param>
    /// <param name="hazardStatements">Optional hazard statements. Duplicates are ignored.</param>
    /// <param name="addedAt">
    /// When the product is added. Use cases pass the time from their clock (TimeProvider);
    /// when left out, the current time is used.
    /// </param>
    /// <returns>The new product with a new <see cref="Id"/>.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> or <paramref name="supplier"/> is empty or longer than <see cref="MaxTextLength"/>.
    /// </exception>
    public static ChemicalProduct Create(
        string name,
        string supplier,
        IEnumerable<HazardStatement>? hazardStatements = null,
        DateTimeOffset? addedAt = null)
    {
        // Version 7 GUIDs are sorted by creation time, which keeps database indexes efficient.
        // Times are stored in UTC; screens convert them to local time.
        var product = new ChemicalProduct(
            Guid.CreateVersion7(),
            ValidateText(name, nameof(name)),
            ValidateText(supplier, nameof(supplier)),
            (addedAt ?? DateTimeOffset.UtcNow).ToUniversalTime());

        foreach (var statement in hazardStatements ?? [])
        {
            product.AddHazardStatement(statement);
        }

        return product;
    }

    /// <summary>Changes the trade name.</summary>
    /// <param name="name">The new name. Surrounding spaces are removed.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or too long.</exception>
    public void Rename(string name) => Name = ValidateText(name, nameof(name));

    /// <summary>Changes the supplier.</summary>
    /// <param name="supplier">The new supplier. Surrounding spaces are removed.</param>
    /// <exception cref="ArgumentException"><paramref name="supplier"/> is empty or too long.</exception>
    public void ChangeSupplier(string supplier) => Supplier = ValidateText(supplier, nameof(supplier));

    /// <summary>
    /// Replaces all details of the product at once, for example after editing it in a form.
    /// </summary>
    /// <remarks>
    /// Every value is validated <b>before</b> anything changes, so a failed update leaves the
    /// product exactly as it was. Duplicates are ignored and the order of the lists is kept.
    /// <see cref="LastChangedAt"/> is only moved when something really changed,
    /// so saving the form without changes keeps the old date.
    /// </remarks>
    /// <param name="name">The new trade name. Surrounding spaces are removed.</param>
    /// <param name="supplier">The new supplier. Surrounding spaces are removed.</param>
    /// <param name="hazardStatements">The complete new list of hazard statements.</param>
    /// <param name="pictograms">The complete new list of pictograms.</param>
    /// <param name="changedAt">When the change is made, from the use case's clock.</param>
    /// <returns><see langword="true"/> if something changed; <see langword="false"/> if all values were the same.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> or <paramref name="supplier"/> is empty or too long.</exception>
    /// <exception cref="ArgumentNullException">A list is <see langword="null"/> or contains <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A pictogram is not a defined GHS pictogram.</exception>
    public bool Update(
        string name,
        string supplier,
        IEnumerable<HazardStatement> hazardStatements,
        IEnumerable<Pictogram> pictograms,
        DateTimeOffset changedAt)
    {
        ArgumentNullException.ThrowIfNull(hazardStatements);
        ArgumentNullException.ThrowIfNull(pictograms);

        var validName = ValidateText(name, nameof(name));
        var validSupplier = ValidateText(supplier, nameof(supplier));

        var newStatements = new List<HazardStatement>();
        foreach (var statement in hazardStatements)
        {
            ArgumentNullException.ThrowIfNull(statement, nameof(hazardStatements));
            if (!newStatements.Contains(statement))
            {
                newStatements.Add(statement);
            }
        }

        var newPictograms = new List<Pictogram>();
        foreach (var pictogram in pictograms)
        {
            PictogramCodes.EnsureDefined(pictogram);
            if (!newPictograms.Contains(pictogram))
            {
                newPictograms.Add(pictogram);
            }
        }

        var changed =
            validName != Name ||
            validSupplier != Supplier ||
            !newStatements.SequenceEqual(_hazardStatements) ||
            !newPictograms.SequenceEqual(_pictograms);
        if (!changed)
        {
            return false;
        }

        // Everything is valid and different: now change the product.
        Name = validName;
        Supplier = validSupplier;
        _hazardStatements = newStatements;
        _pictograms = newPictograms;
        LastChangedAt = changedAt.ToUniversalTime();
        return true;
    }

    /// <summary>Adds a hazard statement unless the product already has it.</summary>
    /// <param name="statement">The hazard statement to add.</param>
    /// <returns><see langword="true"/> if it was added; <see langword="false"/> if it was already there.</returns>
    public bool AddHazardStatement(HazardStatement statement)
    {
        ArgumentNullException.ThrowIfNull(statement);

        if (_hazardStatements.Contains(statement))
        {
            return false;
        }

        _hazardStatements.Add(statement);
        return true;
    }

    /// <summary>Removes a hazard statement.</summary>
    /// <param name="statement">The hazard statement to remove.</param>
    /// <returns><see langword="true"/> if it was removed; <see langword="false"/> if the product did not have it.</returns>
    public bool RemoveHazardStatement(HazardStatement statement)
    {
        ArgumentNullException.ThrowIfNull(statement);
        return _hazardStatements.Remove(statement);
    }

    /// <summary>Adds a GHS pictogram unless the product already has it.</summary>
    /// <param name="pictogram">The pictogram to add.</param>
    /// <returns><see langword="true"/> if it was added; <see langword="false"/> if it was already there.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pictogram"/> is not a defined GHS pictogram.</exception>
    public bool AddPictogram(Pictogram pictogram)
    {
        PictogramCodes.EnsureDefined(pictogram);

        if (_pictograms.Contains(pictogram))
        {
            return false;
        }

        _pictograms.Add(pictogram);
        return true;
    }

    /// <summary>Removes a GHS pictogram.</summary>
    /// <param name="pictogram">The pictogram to remove.</param>
    /// <returns><see langword="true"/> if it was removed; <see langword="false"/> if the product did not have it.</returns>
    public bool RemovePictogram(Pictogram pictogram) => _pictograms.Remove(pictogram);

    private static string ValidateText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var trimmed = value.Trim();
        if (trimmed.Length > MaxTextLength)
        {
            throw new ArgumentException($"Must be at most {MaxTextLength} characters.", parameterName);
        }

        return trimmed;
    }
}
