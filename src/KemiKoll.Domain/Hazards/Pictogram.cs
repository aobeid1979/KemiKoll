namespace KemiKoll.Domain.Hazards;

/// <summary>
/// The nine GHS hazard pictograms used on labels and in section 2 of a safety data sheet.
/// The value names are the official pictogram names from the CLP Regulation (EC) No 1272/2008, Annex V.
/// </summary>
/// <remarks>
/// The numeric values match the official codes (GHS01 = 1 … GHS09 = 9),
/// so the value can be stored in a database and still be read by a human.
/// Use <see cref="PictogramCodes"/> to convert to and from the text code, for example "GHS02".
/// </remarks>
public enum Pictogram
{
    /// <summary>GHS01 – Exploding bomb: explosives, self-reactive substances, organic peroxides.</summary>
    ExplodingBomb = 1,

    /// <summary>GHS02 – Flame: flammable gases, aerosols, liquids and solids; self-heating and pyrophoric substances.</summary>
    Flame = 2,

    /// <summary>GHS03 – Flame over circle: oxidising gases, liquids and solids.</summary>
    FlameOverCircle = 3,

    /// <summary>GHS04 – Gas cylinder: gases under pressure.</summary>
    GasCylinder = 4,

    /// <summary>GHS05 – Corrosion: corrosive to metals, skin corrosion, serious eye damage.</summary>
    Corrosion = 5,

    /// <summary>GHS06 – Skull and crossbones: acute toxicity, categories 1–3.</summary>
    SkullAndCrossbones = 6,

    /// <summary>GHS07 – Exclamation mark: acute toxicity category 4, skin and eye irritation, skin sensitisation, narcotic effects.</summary>
    ExclamationMark = 7,

    /// <summary>GHS08 – Health hazard: carcinogenic, mutagenic, toxic to reproduction, respiratory sensitisation, organ toxicity, aspiration hazard.</summary>
    HealthHazard = 8,

    /// <summary>GHS09 – Environment: hazardous to the aquatic environment.</summary>
    Environment = 9,
}
