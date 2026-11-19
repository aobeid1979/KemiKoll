# 3. Review age of safety data sheets is a company setting

- **Status:** Accepted
- **Date:** 2026-11-15

## Context

KemiKoll warns when a safety data sheet may be outdated. REACH sets no fixed expiry date
for a data sheet: the supplier must update it when new information becomes available.
Companies therefore choose how often they check with the supplier. Options considered:
a fixed 3 years, a fixed 5 years, or a setting per company.

## Decision

Each company sets its own review age (`ReviewPolicy`), from 1 to 10 years.
The default is **3 years**. A data sheet is *due for review* on and after its revision
date plus the review age. When a product has several data sheets, only the newest counts.

Until company accounts exist, every product uses the default.

## Consequences

- Flexible for customers with different routines; safe default for everyone else.
- The rule takes "today" as input (`TimeProvider`), so the exact day boundary is tested.
- The company's own policy must be wired into `ListProducts` when accounts are added.
