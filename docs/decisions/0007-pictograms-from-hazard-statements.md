# 7. Check and suggest pictograms from the hazard statements (CLP)

- **Status:** Accepted
- **Date:** 2026-11-21

## Context

Testing with real data showed two problems:

- Many data sheets show the GHS pictograms only as **images**, so the text rules
  (decision 0006) cannot find them (example: a Swedish petrol data sheet).
- A pictogram can be ticked by mistake (example: GHS03 "flame over circle", which is for
  oxidisers, on ethanol, whose H225 requires GHS02 "flame").

Under CLP (Regulation (EC) No 1272/2008), each hazard class and category determines its
pictogram, and Article 26 gives precedence rules. The required pictograms can therefore be
derived from the hazard statements.

## Decision

`PictogramRules` (Domain) maps each hazard statement to its required pictograms and applies
the Article 26 precedence rules:

- GHS06 applies → GHS07 is not used.
- GHS05 applies → GHS07 is not used for skin or eye irritation (H315, H319).
- H334 (respiratory sensitisation, GHS08) → GHS07 is not used for skin sensitisation (H317)
  or skin/eye irritation.
- GHS07 stays when another reason remains (for example H336).

It is used in three places:

1. **PDF extraction:** when no pictogram codes are written in section 2, the pictograms are
   suggested from the H-codes, with a warning to compare them with the symbols.
2. **Add product form:** a live check shows missing and unexpected pictograms, with a button
   to use the matching ones. Saving is **not** blocked: the user decides.
3. **Product list:** "Missing: …" / "Not expected: …" under the pictograms.

Codes whose pictogram cannot be decided from the code alone (for example H205–H211 for
explosives) are **not guessed**: they are listed for the user to check by hand, and no
"unexpected" pictograms are reported while such a code is present.

The optional-pictogram rule for GHS01 (GHS02/GHS03 may be left out) is not applied:
the check suggests the full set.

## Consequences

- Wrong or missing pictograms are visible immediately, in the form and in the list.
- Data sheets with pictogram images get a correct suggestion from their H-codes.
- The table must be kept in line with CLP when new hazard classes or statements are added.
- The table was written from the CLP rules and reviewed by the project owner (chemist);
  it is a support tool, not a legal classification.
