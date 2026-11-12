namespace KemiKoll.Domain.Hazards;

/// <summary>
/// The group a hazard statement belongs to, as defined by the CLP Regulation (EC) No 1272/2008.
/// </summary>
public enum HazardCategory
{
    /// <summary>Physical hazards, codes H200–H299 (for example H225 "Highly flammable liquid and vapour").</summary>
    Physical,

    /// <summary>Health hazards, codes H300–H399 (for example H315 "Causes skin irritation").</summary>
    Health,

    /// <summary>Environmental hazards, codes H400–H499 (for example H410 "Very toxic to aquatic life with long lasting effects").</summary>
    Environmental,

    /// <summary>Supplemental EU hazard statements, codes starting with EUH (for example EUH066 "Repeated exposure may cause skin dryness or cracking").</summary>
    SupplementalEu,
}
