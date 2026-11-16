using KemiKoll.Domain.SafetyDataSheets;

namespace KemiKoll.Domain.Companies;

/// <summary>
/// A customer company, for example a car workshop or a CNC shop,
/// with its own settings and workplaces.
/// </summary>
/// <remarks>
/// The company is the owner (aggregate root) of its <see cref="Workplaces"/>:
/// all changes to workplaces go through the company, so rules that involve
/// several workplaces (such as unique names) are always checked.
/// </remarks>
public sealed class Company
{
    /// <summary>The maximum length of <see cref="Name"/>.</summary>
    public const int MaxNameLength = 200;

    private readonly List<Workplace> _workplaces = [];

    private Company(Guid id, string name, OrganizationNumber organizationNumber)
    {
        Id = id;
        Name = name;
        OrganizationNumber = organizationNumber;
    }

    /// <summary>The unique identity of the company.</summary>
    public Guid Id { get; }

    /// <summary>The company name, for example "Bilverkstad i Jönköping AB".</summary>
    public string Name { get; private set; }

    /// <summary>The Swedish organisation number.</summary>
    public OrganizationNumber OrganizationNumber { get; }

    /// <summary>
    /// How old a safety data sheet may be before it is due for review.
    /// Starts as <see cref="ReviewPolicy.Default"/> (3 years).
    /// </summary>
    public ReviewPolicy ReviewPolicy { get; private set; } = ReviewPolicy.Default;

    /// <summary>The company's workplaces, in the order they were added.</summary>
    public IReadOnlyList<Workplace> Workplaces => _workplaces;

    /// <summary>
    /// Creates a new company with the default review policy and no workplaces.
    /// </summary>
    /// <param name="name">The company name. Surrounding spaces are removed.</param>
    /// <param name="organizationNumber">The Swedish organisation number.</param>
    /// <returns>The new company.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or too long.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="organizationNumber"/> is <see langword="null"/>.</exception>
    public static Company Create(string name, OrganizationNumber organizationNumber)
    {
        ArgumentNullException.ThrowIfNull(organizationNumber);

        return new Company(Guid.CreateVersion7(), ValidateName(name, MaxNameLength, nameof(name)), organizationNumber);
    }

    /// <summary>Changes the company name.</summary>
    /// <param name="name">The new name. Surrounding spaces are removed.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or too long.</exception>
    public void Rename(string name) => Name = ValidateName(name, MaxNameLength, nameof(name));

    /// <summary>Sets how old a safety data sheet may be before it is due for review.</summary>
    /// <param name="policy">The new policy.</param>
    /// <exception cref="ArgumentNullException"><paramref name="policy"/> is <see langword="null"/>.</exception>
    public void ChangeReviewPolicy(ReviewPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ReviewPolicy = policy;
    }

    /// <summary>Adds a new workplace.</summary>
    /// <param name="name">The workplace name. Surrounding spaces are removed.</param>
    /// <returns>The new workplace.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is empty, too long, or already used by another workplace of this company.
    /// </exception>
    public Workplace AddWorkplace(string name)
    {
        var validName = ValidateWorkplaceName(name, excludeId: null);

        var workplace = new Workplace(Guid.CreateVersion7(), validName);
        _workplaces.Add(workplace);
        return workplace;
    }

    /// <summary>Renames one of the company's workplaces.</summary>
    /// <param name="workplaceId">The id of the workplace.</param>
    /// <param name="name">The new name. Surrounding spaces are removed.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is empty, too long, or already used by another workplace of this company.
    /// </exception>
    /// <exception cref="KeyNotFoundException">The company has no workplace with <paramref name="workplaceId"/>.</exception>
    public void RenameWorkplace(Guid workplaceId, string name)
    {
        var workplace = FindWorkplace(workplaceId)
            ?? throw new KeyNotFoundException($"The company has no workplace with id {workplaceId}.");

        workplace.Rename(ValidateWorkplaceName(name, excludeId: workplaceId));
    }

    /// <summary>Removes one of the company's workplaces.</summary>
    /// <param name="workplaceId">The id of the workplace.</param>
    /// <returns><see langword="true"/> if it was removed; <see langword="false"/> if the company did not have it.</returns>
    public bool RemoveWorkplace(Guid workplaceId)
    {
        var workplace = FindWorkplace(workplaceId);
        return workplace is not null && _workplaces.Remove(workplace);
    }

    private Workplace? FindWorkplace(Guid workplaceId) => _workplaces.Find(w => w.Id == workplaceId);

    private string ValidateWorkplaceName(string name, Guid? excludeId)
    {
        var validName = ValidateName(name, Workplace.MaxNameLength, nameof(name));

        // "Workshop" and "workshop" would confuse users, so names are compared without case.
        var isTaken = _workplaces.Exists(w =>
            w.Id != excludeId && string.Equals(w.Name, validName, StringComparison.OrdinalIgnoreCase));

        if (isTaken)
        {
            throw new ArgumentException($"A workplace named '{validName}' already exists.", nameof(name));
        }

        return validName;
    }

    private static string ValidateName(string value, int maxLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Must be at most {maxLength} characters.", parameterName);
        }

        return trimmed;
    }
}
