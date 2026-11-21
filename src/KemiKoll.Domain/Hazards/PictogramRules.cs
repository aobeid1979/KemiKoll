using System.Collections.Frozen;

namespace KemiKoll.Domain.Hazards;

/// <summary>
/// Which GHS pictograms a product must carry, derived from its hazard statements,
/// following the CLP Regulation (EC) No 1272/2008 (Annex I and the precedence rules in Article 26).
/// </summary>
/// <remarks>
/// <para>
/// Each hazard statement belongs to a hazard class and category, and CLP assigns a pictogram
/// (or none) to it. For example H225 (flammable liquid, category 2) requires GHS02.
/// </para>
/// <para>
/// Codes whose pictogram depends on details not visible in the code alone are deliberately
/// <b>not</b> in the table (for example H205–H211 for explosives). They are reported as
/// <see cref="PictogramRequirement.CodesWithoutRule"/> so the user checks them by hand
/// instead of the system guessing.
/// </para>
/// </remarks>
public static class PictogramRules
{
    // Hazard statement (without suffix) → required pictograms. An empty array means
    // "no pictogram for this code" (for example H412, H229 or H362).
    private static readonly FrozenDictionary<string, Pictogram[]> Table = new Dictionary<string, Pictogram[]>
    {
        // Physical hazards
        ["H200"] = [Pictogram.ExplodingBomb], // unstable explosive
        ["H201"] = [Pictogram.ExplodingBomb],
        ["H202"] = [Pictogram.ExplodingBomb],
        ["H203"] = [Pictogram.ExplodingBomb],
        ["H204"] = [Pictogram.ExplodingBomb],
        ["H220"] = [Pictogram.Flame], // flammable gas, category 1
        ["H221"] = [], // flammable gas, category 2: no pictogram
        ["H222"] = [Pictogram.Flame], // aerosol, category 1
        ["H223"] = [Pictogram.Flame], // aerosol, category 2
        ["H224"] = [Pictogram.Flame], // flammable liquid, category 1
        ["H225"] = [Pictogram.Flame], // flammable liquid, category 2
        ["H226"] = [Pictogram.Flame], // flammable liquid, category 3
        ["H228"] = [Pictogram.Flame], // flammable solid
        ["H229"] = [], // aerosol, category 3 (pressurised container): no pictogram
        ["H230"] = [], // chemically unstable gas: supplements a flammable-gas statement
        ["H231"] = [],
        ["H232"] = [Pictogram.Flame], // pyrophoric gas
        ["H240"] = [Pictogram.ExplodingBomb], // self-reactive / organic peroxide, type A
        ["H241"] = [Pictogram.ExplodingBomb, Pictogram.Flame], // type B
        ["H242"] = [Pictogram.Flame], // types C–F
        ["H250"] = [Pictogram.Flame], // pyrophoric liquid/solid
        ["H251"] = [Pictogram.Flame], // self-heating
        ["H252"] = [Pictogram.Flame],
        ["H260"] = [Pictogram.Flame], // in contact with water releases flammable gases
        ["H261"] = [Pictogram.Flame],
        ["H270"] = [Pictogram.FlameOverCircle], // oxidising gas
        ["H271"] = [Pictogram.FlameOverCircle], // oxidising liquid/solid
        ["H272"] = [Pictogram.FlameOverCircle],
        ["H280"] = [Pictogram.GasCylinder], // gas under pressure
        ["H281"] = [Pictogram.GasCylinder], // refrigerated liquefied gas
        ["H290"] = [Pictogram.Corrosion], // corrosive to metals

        // Health hazards
        ["H300"] = [Pictogram.SkullAndCrossbones], // acute toxicity, categories 1–3
        ["H301"] = [Pictogram.SkullAndCrossbones],
        ["H310"] = [Pictogram.SkullAndCrossbones],
        ["H311"] = [Pictogram.SkullAndCrossbones],
        ["H330"] = [Pictogram.SkullAndCrossbones],
        ["H331"] = [Pictogram.SkullAndCrossbones],
        ["H302"] = [Pictogram.ExclamationMark], // acute toxicity, category 4
        ["H312"] = [Pictogram.ExclamationMark],
        ["H332"] = [Pictogram.ExclamationMark],
        ["H304"] = [Pictogram.HealthHazard], // aspiration hazard
        ["H314"] = [Pictogram.Corrosion], // skin corrosion
        ["H318"] = [Pictogram.Corrosion], // serious eye damage
        ["H315"] = [Pictogram.ExclamationMark], // skin irritation
        ["H317"] = [Pictogram.ExclamationMark], // skin sensitisation
        ["H319"] = [Pictogram.ExclamationMark], // eye irritation
        ["H334"] = [Pictogram.HealthHazard], // respiratory sensitisation
        ["H335"] = [Pictogram.ExclamationMark], // STOT SE 3, respiratory irritation
        ["H336"] = [Pictogram.ExclamationMark], // STOT SE 3, narcotic effects
        ["H340"] = [Pictogram.HealthHazard], // germ cell mutagenicity
        ["H341"] = [Pictogram.HealthHazard],
        ["H350"] = [Pictogram.HealthHazard], // carcinogenicity
        ["H351"] = [Pictogram.HealthHazard],
        ["H360"] = [Pictogram.HealthHazard], // reproductive toxicity (all F/D/f/d variants)
        ["H361"] = [Pictogram.HealthHazard],
        ["H362"] = [], // effects on or via lactation: no pictogram
        ["H370"] = [Pictogram.HealthHazard], // STOT SE 1–2
        ["H371"] = [Pictogram.HealthHazard],
        ["H372"] = [Pictogram.HealthHazard], // STOT RE 1–2
        ["H373"] = [Pictogram.HealthHazard],

        // Environmental hazards
        ["H400"] = [Pictogram.Environment], // aquatic acute 1
        ["H410"] = [Pictogram.Environment], // aquatic chronic 1
        ["H411"] = [Pictogram.Environment], // aquatic chronic 2
        ["H412"] = [], // aquatic chronic 3: no pictogram
        ["H413"] = [], // aquatic chronic 4: no pictogram
        ["H420"] = [Pictogram.ExclamationMark], // hazardous to the ozone layer
    }.ToFrozenDictionary();

    // Skin and eye irritation: GHS07 is dropped for these when GHS05 or H334 applies.
    private static readonly FrozenSet<string> IrritationCodes = new[] { "H315", "H319" }.ToFrozenSet();

    /// <summary>Works out the pictograms that the given hazard statements require.</summary>
    /// <param name="statements">The product's hazard statements.</param>
    /// <returns>The required pictograms (official order) and the codes without a rule.</returns>
    public static PictogramRequirement For(IEnumerable<HazardStatement> statements)
    {
        ArgumentNullException.ThrowIfNull(statements);

        var required = new HashSet<Pictogram>();
        var exclamationMarkReasons = new HashSet<string>();
        var codesWithoutRule = new List<string>();
        var hasRespiratorySensitisation = false;

        foreach (var statement in statements)
        {
            // Supplemental EU statements (EUH…) never carry a pictogram of their own.
            if (statement.Category == HazardCategory.SupplementalEu)
            {
                continue;
            }

            // "H360FD" and "H361d" follow the rule of their base code.
            var baseCode = statement.Code[..4];
            hasRespiratorySensitisation |= baseCode == "H334";
            if (!Table.TryGetValue(baseCode, out var pictograms))
            {
                codesWithoutRule.Add(statement.Code);
                continue;
            }

            foreach (var pictogram in pictograms)
            {
                required.Add(pictogram);
                if (pictogram == Pictogram.ExclamationMark)
                {
                    exclamationMarkReasons.Add(baseCode);
                }
            }
        }

        ApplyPrecedenceRules(required, exclamationMarkReasons, hasRespiratorySensitisation);

        return new PictogramRequirement(
            Enum.GetValues<Pictogram>().Where(required.Contains).ToList(),
            codesWithoutRule);
    }

    // CLP Article 26(1):
    // - if GHS06 applies, GHS07 shall not appear;
    // - if GHS05 applies, GHS07 shall not appear for skin or eye irritation;
    // - if GHS08 applies for respiratory sensitisation (H334), GHS07 shall not appear
    //   for skin sensitisation or for skin or eye irritation.
    // GHS07 stays when another reason for it remains (for example H336).
    private static void ApplyPrecedenceRules(
        HashSet<Pictogram> required,
        HashSet<string> exclamationMarkReasons,
        bool hasRespiratorySensitisation)
    {
        if (required.Contains(Pictogram.SkullAndCrossbones))
        {
            exclamationMarkReasons.Clear();
        }

        if (required.Contains(Pictogram.Corrosion))
        {
            exclamationMarkReasons.ExceptWith(IrritationCodes);
        }

        if (hasRespiratorySensitisation)
        {
            exclamationMarkReasons.ExceptWith(IrritationCodes);
            exclamationMarkReasons.Remove("H317");
        }

        if (exclamationMarkReasons.Count == 0)
        {
            required.Remove(Pictogram.ExclamationMark);
        }
    }
}
