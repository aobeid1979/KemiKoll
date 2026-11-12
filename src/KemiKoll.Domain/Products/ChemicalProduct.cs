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
    private readonly List<HazardStatement> _hazardStatements = [];

    private ChemicalProduct(Guid id, string name, string supplier)
    {
        Id = id;
        Name = name;
        Supplier = supplier;
    }

    /// <summary>The unique identity of the product.</summary>
    public Guid Id { get; }

    /// <summary>The trade name as written on the safety data sheet, for example "Hycut ET 46".</summary>
    public string Name { get; private set; }

    /// <summary>The company that supplies the product and issues its safety data sheet.</summary>
    public string Supplier { get; private set; }

    /// <summary>The hazard statements (H-phrases) of the product, in the order they were added.</summary>
    public IReadOnlyList<HazardStatement> HazardStatements => _hazardStatements;

    /// <summary>
    /// <see langword="true"/> if the product has at least one hazard statement.
    /// Hazardous products need a safety data sheet and a risk assessment.
    /// </summary>
    public bool IsHazardous => _hazardStatements.Count > 0;

    /// <summary>
    /// Creates a new chemical product.
    /// </summary>
    /// <param name="name">The trade name. Surrounding spaces are removed.</param>
    /// <param name="supplier">The supplier. Surrounding spaces are removed.</param>
    /// <param name="hazardStatements">Optional hazard statements. Duplicates are ignored.</param>
    /// <returns>The new product with a new <see cref="Id"/>.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> or <paramref name="supplier"/> is empty or longer than <see cref="MaxTextLength"/>.
    /// </exception>
    public static ChemicalProduct Create(
        string name,
        string supplier,
        IEnumerable<HazardStatement>? hazardStatements = null)
    {
        // Version 7 GUIDs are sorted by creation time, which keeps database indexes efficient.
        var product = new ChemicalProduct(
            Guid.CreateVersion7(),
            ValidateText(name, nameof(name)),
            ValidateText(supplier, nameof(supplier)));

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
