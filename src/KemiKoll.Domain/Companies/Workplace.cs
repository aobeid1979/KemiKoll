namespace KemiKoll.Domain.Companies;

/// <summary>
/// A place where a company uses or stores chemical products,
/// for example "Workshop", "Paint booth" or "Chemical cabinet 1".
/// </summary>
/// <remarks>
/// A workplace always belongs to one <see cref="Company"/> and is created and changed
/// only through that company. This lets the company enforce that workplace names are unique.
/// </remarks>
public sealed class Workplace
{
    /// <summary>The maximum length of <see cref="Name"/>.</summary>
    public const int MaxNameLength = 100;

    internal Workplace(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>The unique identity of the workplace.</summary>
    public Guid Id { get; }

    /// <summary>The name of the workplace, unique within its company.</summary>
    public string Name { get; private set; }

    internal void Rename(string name) => Name = name;
}
