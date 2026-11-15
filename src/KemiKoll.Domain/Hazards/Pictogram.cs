namespace KemiKoll.Domain.Hazards;

/// <summary>
/// The nine GHS hazard pictograms used on labels and in section 2 of a safety data sheet,
/// as defined by the CLP Regulation (EC) No 1272/2008, Annex V.
/// </summary>
/// <remarks>
/// The numeric values match the official codes (GHS01 = 1 … GHS09 = 9),
/// so the value can be stored in a database and still be read by a human.
/// Use <see cref="PictogramCodes"/> to convert to and from the text code, for example "GHS02".
/// </remarks>
public enum Pictogram
{
    /// <summary>GHS01 – Exploding bomb: explosives and self-reactive substances.</summary>
    Explosive = 1,

    /// <summary>GHS02 – Flame: flammable gases, liquids and solids.</summary>
    Flammable = 2,

    /// <summary>GHS03 – Flame over circle: oxidising substances.</summary>
    Oxidising = 3,

    /// <summary>GHS04 – Gas cylinder: gases under pressure.</summary>
    GasUnderPressure = 4,

    /// <summary>GHS05 – Corrosion: corrosive to metals, skin corrosion, serious eye damage.</summary>
    Corrosive = 5,

    /// <summary>GHS06 – Skull and crossbones: acute toxicity (severe).</summary>
    AcuteToxicity = 6,

    /// <summary>GHS07 – Exclamation mark: irritant, sensitiser, harmful, narcotic effects.</summary>
    HealthHazardLow = 7,

    /// <summary>GHS08 – Health hazard: carcinogenic, mutagenic, toxic to reproduction, respiratory sensitiser, aspiration hazard.</summary>
    SeriousHealthHazard = 8,

    /// <summary>GHS09 – Environment: hazardous to the aquatic environment.</summary>
    Environment = 9,
}
