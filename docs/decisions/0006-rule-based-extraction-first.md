# 6. Extract data sheet facts with fixed rules first, AI later

- **Status:** Accepted
- **Date:** 2026-11-21

## Context

An uploaded safety data sheet should fill in the product form. Two options:

1. Fixed rules (regular expressions) for the facts that follow strict patterns, AI later for the rest.
2. An AI model for everything from the start.

AI is more flexible, but every PDF would cost money and need an API key from day one,
before there is a single paying customer.

## Decision

Start with **fixed rules** (`SafetyDataSheetTextParser`, no external service):

- **Hazard codes** (H200–H499 with CLP suffixes, EUH) and **pictogram codes** (GHS01–GHS09)
  are taken from **section 2 only**. Section 3 lists the codes of each ingredient and section 16
  repeats them; those are not the classification of the product.
- **Revision date** from labels such as "Revisionsdatum" / "Revision date"; the issue date only if
  there is no revision date.
- **Language** (sv/en), **product name** and **supplier** from common labels in section 1.
- Everything is a **suggestion**: the user checks it in the form before saving. Uncertain results
  come with a warning.

AI can be added later behind the same result type, for example only for the fields the rules
could not find.

## Consequences

- Free, works offline, fast, and fully covered by unit tests.
- Data sheets with unusual layouts or wording may give incomplete suggestions;
  the user fills in the rest.
- Pictograms shown only as images (without the text code) are not found.
- Scanned PDFs (images only) cannot be read; the user is told to ask for a digital version.
